using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Application.Features.Invoices.Services;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.CreateInvoice;

public record CreateAdjustmentInvoiceCommand(
    Guid DonviId,
    Guid KhachhangId,
    Guid MauctyId,
    DateTime Ngaylap,
    IReadOnlyCollection<InvoiceLineRequestDto> Hanghoas,
    Guid? ThamChieuHoadonId = null) : IRequest<CreateInvoiceResultDto>;

public class CreateAdjustmentInvoiceCommandValidator : AbstractValidator<CreateAdjustmentInvoiceCommand>
{
    public CreateAdjustmentInvoiceCommandValidator()
    {
        RuleFor(x => x.DonviId).NotEmpty();
        RuleFor(x => x.KhachhangId).NotEmpty();
        RuleFor(x => x.MauctyId).NotEmpty();
        RuleFor(x => x.Ngaylap).NotEmpty();
        RuleFor(x => x.Hanghoas).NotEmpty();
        RuleFor(x => x.ThamChieuHoadonId).NotNull();
        RuleForEach(x => x.Hanghoas).SetValidator(new InvoiceLineRequestValidator());
    }
}

public class CreateAdjustmentInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    ICompanyRepository companyRepository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateAdjustmentInvoiceCommand, CreateInvoiceResultDto>
{
    private const string DraftStatus = "Draft";

    public async Task<CreateInvoiceResultDto> Handle(CreateAdjustmentInvoiceCommand request, CancellationToken cancellationToken)
    {
        if (!request.ThamChieuHoadonId.HasValue)
        {
            throw new InvalidOperationException("Thiếu hóa đơn gốc để lập điều chỉnh.");
        }

        var sourceInvoice = await invoiceRepository.GetByIdAsync(request.ThamChieuHoadonId.Value, cancellationToken);
        if (sourceInvoice is null || sourceInvoice.IsDeleted)
        {
            throw new InvalidOperationException("Không tìm thấy hóa đơn gốc.");
        }

        if (sourceInvoice.Donviid != request.DonviId)
        {
            throw new InvalidOperationException("Hóa đơn điều chỉnh phải cùng đơn vị với hóa đơn gốc.");
        }

        if (sourceInvoice.Khachhangid != request.KhachhangId)
        {
            throw new InvalidOperationException("Khách hàng của hóa đơn điều chỉnh phải khớp với hóa đơn gốc.");
        }

        if (sourceInvoice.Mauctyid != request.MauctyId)
        {
            throw new InvalidOperationException("Mẫu công ty của hóa đơn điều chỉnh phải khớp với hóa đơn gốc.");
        }

        var status = sourceInvoice.Trangthai.Trim();
        if (status is not ("Issued" or "Signed" or "PendingSign" or "Draft"))
        {
            throw new InvalidOperationException("Hóa đơn gốc không ở trạng thái cho phép lập điều chỉnh.");
        }

        var customer = await customerRepository.GetByIdAsync(request.KhachhangId, cancellationToken);
        if (customer is null || customer.IsDeleted || customer.Donviid != request.DonviId)
        {
            throw new InvalidOperationException("Khách hàng không hợp lệ hoặc không thuộc đơn vị.");
        }

        var seller = await companyRepository.GetByIdAsync(request.DonviId, cancellationToken)
            ?? throw new InvalidOperationException("Đơn vị bán hàng không tồn tại.");

        var productMap = await ValidateProductsAsync(request.DonviId, request.Hanghoas, cancellationToken);

        var invoiceId = Guid.NewGuid();
        var now = dateTimeProvider.UtcNow;

        var todayCount = await invoiceRepository.GetInvoiceCountByDateAsync(request.DonviId, request.Ngaylap, cancellationToken);
        var nextNumber = todayCount + 1;
        var generatedSoHoadon = $"HD{nextNumber:D2}-{request.Ngaylap:ddMMyyyy}";

        var lineItems = BuildLineItems(invoiceId, request.Hanghoas, productMap);
        var (tongtien, tienthue, tongthanhtoan) = CalculateTotals(lineItems);

        var invoice = new Hoadon
        {
            Id = invoiceId,
            Donviid = request.DonviId,
            Khachhangid = request.KhachhangId,
            Mauctyid = request.MauctyId,
            Kyhieu = null,
            Sohoadon = generatedSoHoadon,
            Ngaylap = request.Ngaylap,
            Tongtien = tongtien,
            Tienthue = tienthue,
            Tongthanhtoan = tongthanhtoan,
            Trangthai = DraftStatus,
            Thamchieuhoadonid = request.ThamChieuHoadonId,
            Xmldaky = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        invoice.XmlMetadata = InvoiceXmlBuilder.Build(invoice, lineItems, productMap, seller, customer);

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = invoiceId,
            Hanhdong = "Lập hóa đơn điều chỉnh",
            Trangthaicu = null,
            Trangthaimoi = DraftStatus,
            Thoigian = now,
            Nguoidungid = currentUserService.UserId
        };

        await invoiceRepository.CreateDraftInvoiceAsync(invoice, lineItems, history, cancellationToken);

        return new CreateInvoiceResultDto
        {
            Id = invoiceId,
            Trangthai = DraftStatus,
            Tongtien = tongtien,
            Tienthue = tienthue,
            Tongthanhtoan = tongthanhtoan
        };
    }

    private async Task<Dictionary<Guid, Danhmuchanghoa>> ValidateProductsAsync(
        Guid donviId,
        IReadOnlyCollection<InvoiceLineRequestDto> lines,
        CancellationToken cancellationToken)
    {
        var productMap = new Dictionary<Guid, Danhmuchanghoa>();
        foreach (var productId in lines.Select(x => x.HanghoaId).Distinct())
        {
            var product = await productRepository.GetByIdAsync(productId, cancellationToken);
            if (product is null || product.IsDeleted || product.Donviid != donviId)
            {
                throw new InvalidOperationException($"Hàng hóa {productId} không hợp lệ hoặc không thuộc đơn vị.");
            }

            productMap[productId] = product;
        }

        return productMap;
    }

    private static IReadOnlyCollection<HoadonHanghoa> BuildLineItems(
        Guid invoiceId,
        IReadOnlyCollection<InvoiceLineRequestDto> lines,
        IReadOnlyDictionary<Guid, Danhmuchanghoa> productMap)
    {
        var results = new List<HoadonHanghoa>(lines.Count);
        foreach (var line in lines)
        {
            var unitPrice = line.Dongia > 0 ? line.Dongia : productMap[line.HanghoaId].Dongia;
            var thanhtien = line.Soluong * unitPrice;
            results.Add(new HoadonHanghoa
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

        return results;
    }

    private static (decimal tongTien, decimal tienThue, decimal tongThanhToan) CalculateTotals(IReadOnlyCollection<HoadonHanghoa> lineItems)
    {
        var tongTien = lineItems.Sum(x => x.Thanhtien);
        var tienThue = lineItems.Sum(x => x.Thanhtien * x.Thuesuat / 100m);
        return (tongTien, tienThue, tongTien + tienThue);
    }
}