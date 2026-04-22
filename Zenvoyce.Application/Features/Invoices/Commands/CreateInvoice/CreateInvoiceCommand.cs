using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Invoices.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.CreateInvoice;

public record CreateInvoiceCommand(
    Guid DonviId,
    Guid KhachhangId,
    Guid MauctyId,
    string? Kyhieu,
    DateTime Ngaylap,
    IReadOnlyCollection<InvoiceLineRequestDto> Hanghoas) : IRequest<CreateInvoiceResultDto>;

public class CreateInvoiceCommandValidator : AbstractValidator<CreateInvoiceCommand>
{
    public CreateInvoiceCommandValidator()
    {
        RuleFor(x => x.DonviId).NotEmpty();
        RuleFor(x => x.KhachhangId).NotEmpty();
        RuleFor(x => x.MauctyId).NotEmpty();
        RuleFor(x => x.Hanghoas).NotEmpty();
        RuleForEach(x => x.Hanghoas).SetValidator(new InvoiceLineRequestValidator());
    }
}

public class InvoiceLineRequestValidator : AbstractValidator<InvoiceLineRequestDto>
{
    public InvoiceLineRequestValidator()
    {
        RuleFor(x => x.HanghoaId).NotEmpty();
        RuleFor(x => x.Soluong).GreaterThan(0);
        RuleFor(x => x.Dongia).GreaterThan(0);
        RuleFor(x => x.ThueSuat).InclusiveBetween(0, 100);
    }
}

public class CreateInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IProductRepository productRepository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<CreateInvoiceCommand, CreateInvoiceResultDto>
{
    private const string DraftStatus = "Draft";

    public async Task<CreateInvoiceResultDto> Handle(CreateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var customer = await customerRepository.GetByIdAsync(request.KhachhangId, cancellationToken);
        if (customer is null || customer.IsDeleted || customer.Donviid != request.DonviId)
        {
            throw new InvalidOperationException("Khách hàng không hợp lệ hoặc không thuộc đơn vị.");
        }

        var productMap = await ValidateProductsAsync(request.DonviId, request.Hanghoas, cancellationToken);

        var invoiceId = Guid.NewGuid();
        var now = dateTimeProvider.UtcNow;

        var lineItems = BuildLineItems(invoiceId, request.Hanghoas, productMap);
        var (tongtien, tienthue, tongthanhtoan) = CalculateTotals(lineItems);

        var invoice = new Hoadon
        {
            Id = invoiceId,
            Donviid = request.DonviId,
            Khachhangid = request.KhachhangId,
            Mauctyid = request.MauctyId,
            Kyhieu = string.IsNullOrWhiteSpace(request.Kyhieu) ? null : request.Kyhieu.Trim(),
            Sohoadon = null,
            Ngaylap = request.Ngaylap,
            Tongtien = tongtien,
            Tienthue = tienthue,
            Tongthanhtoan = tongthanhtoan,
            Trangthai = DraftStatus,
            Xmldaky = null,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = invoiceId,
            Hanhdong = "Tạo hóa đơn mới",
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
