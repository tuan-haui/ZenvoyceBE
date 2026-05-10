using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Abstractions.Services;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Application.Features.Invoices.Services;
using Zenvoyce.Domain.Entities;

namespace Zenvoyce.Application.Features.Invoices.Queries.PreviewInvoicePdfFromData;

public record PreviewInvoicePdfFromDataQuery(
    Guid DonviId,
    Guid KhachhangId,
    Guid MauctyId,
    DateTime Ngaylap,
    IReadOnlyCollection<InvoiceLineRequestDto> Hanghoas,
    Guid? ThamChieuHoadonId = null) : IRequest<InvoicePreviewResultDto>;

public class PreviewInvoicePdfFromDataQueryValidator : AbstractValidator<PreviewInvoicePdfFromDataQuery>
{
    public PreviewInvoicePdfFromDataQueryValidator()
    {
        RuleFor(x => x.DonviId).NotEmpty();
        RuleFor(x => x.KhachhangId).NotEmpty();
        RuleFor(x => x.MauctyId).NotEmpty();
        RuleFor(x => x.Hanghoas).NotEmpty();
        RuleForEach(x => x.Hanghoas).ChildRules(line =>
        {
            line.RuleFor(x => x.HanghoaId).NotEmpty();
            line.RuleFor(x => x.Soluong).GreaterThan(0);
            line.RuleFor(x => x.Dongia).GreaterThan(0);
            line.RuleFor(x => x.ThueSuat).InclusiveBetween(0, 100);
        });
    }
}

public class PreviewInvoicePdfFromDataQueryHandler(
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    ICompanyRepository companyRepository,
    ITemplateRepository templateRepository,
    ITemplateRenderer templateRenderer,
    IInvoicePdfRenderer pdfRenderer)
    : IRequestHandler<PreviewInvoicePdfFromDataQuery, InvoicePreviewResultDto>
{
    public async Task<InvoicePreviewResultDto> Handle(PreviewInvoicePdfFromDataQuery request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.KhachhangId, cancellationToken);
        if (customer is null || customer.IsDeleted || customer.Donviid != request.DonviId)
        {
            throw new InvalidOperationException("Khách hàng không hợp lệ hoặc không thuộc đơn vị.");
        }

        var seller = await companyRepository.GetByIdAsync(request.DonviId, cancellationToken)
            ?? throw new InvalidOperationException("Đơn vị bán hàng không tồn tại.");

        var companyTemplate = await templateRepository.GetCompanyTemplateByIdAsync(request.MauctyId, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu công ty.");

        var baseTemplate = await templateRepository.GetBaseTemplateByIdAsync(companyTemplate.Maugocid, cancellationToken)
            ?? throw new InvalidOperationException("Không tìm thấy mẫu hóa đơn gốc.");

        if (string.IsNullOrWhiteSpace(baseTemplate.HtmlContent))
        {
            throw new InvalidOperationException("Mẫu hóa đơn gốc chưa có HTML content.");
        }

        var productMap = new Dictionary<Guid, Danhmuchanghoa>();
        foreach (var productId in request.Hanghoas.Select(x => x.HanghoaId).Distinct())
        {
            var product = await productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null || product.IsDeleted || product.Donviid != request.DonviId)
            {
                throw new InvalidOperationException($"Hàng hóa {productId} không hợp lệ hoặc không thuộc đơn vị.");
            }
            productMap[productId] = product;
        }

        var invoiceId = Guid.NewGuid();
        var generatedSoHoadon = $"HD00-{request.Ngaylap:ddMMyyyy}"; // Giả lập số hóa đơn cho preview

        var lineItems = new List<HoadonHanghoa>(request.Hanghoas.Count);
        foreach (var line in request.Hanghoas)
        {
            var unitPrice = line.Dongia > 0 ? line.Dongia : productMap[line.HanghoaId].Dongia;
            var thanhtien = line.Soluong * unitPrice;
            lineItems.Add(new HoadonHanghoa
            {
                Id = Guid.NewGuid(),
                Hoadonid = invoiceId,
                Hanghoaid = line.HanghoaId,
                Soluong = line.Soluong,
                Dongia = unitPrice,
                Thuesuat = line.ThueSuat,
                Thanhtien = thanhtien
            });
        }

        var tongtien = lineItems.Sum(x => x.Thanhtien);
        var tienthue = lineItems.Sum(x => x.Thanhtien * x.Thuesuat / 100m);
        var tongthanhtoan = tongtien + tienthue;

        var dummyInvoice = new Hoadon
        {
            Id = invoiceId,
            Donviid = request.DonviId,
            Khachhangid = request.KhachhangId,
            Mauctyid = request.MauctyId,
            Sohoadon = generatedSoHoadon,
            Ngaylap = request.Ngaylap,
            Tongtien = tongtien,
            Tienthue = tienthue,
            Tongthanhtoan = tongthanhtoan,
            Trangthai = "Draft"
        };

        var xmlMetadata = InvoiceXmlBuilder.Build(dummyInvoice, lineItems, productMap, seller, customer);

        var context = InvoiceXmlContextMapper.Map(xmlMetadata);
        var renderedHtml = templateRenderer.Render(baseTemplate.HtmlContent, context);

        var combinedCss = JoinCss(baseTemplate.CssContent, companyTemplate.Css);
        var pdfBytes = await pdfRenderer.RenderPdfAsync(renderedHtml, combinedCss, cancellationToken);

        return new InvoicePreviewResultDto
        {
            PdfBytes = pdfBytes,
            Filename = "preview.pdf",
            ContentType = "application/pdf"
        };
    }

    private static string? JoinCss(string? baseCss, string? companyCss)
    {
        if (string.IsNullOrWhiteSpace(baseCss) && string.IsNullOrWhiteSpace(companyCss))
        {
            return null;
        }

        return $"{baseCss}\n{companyCss}";
    }
}
