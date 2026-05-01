using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Invoices.Commands.SignInvoice;

public record SignInvoiceCommand(Guid InvoiceId) : IRequest<SignInvoiceResultDto>;

public class SignInvoiceResultDto
{
    public Guid Id { get; set; }
    public string Trangthai { get; set; } = string.Empty;
    public string XmlDaKy { get; set; } = string.Empty;
}

public class SignInvoiceCommandValidator : AbstractValidator<SignInvoiceCommand>
{
    public SignInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty();
    }
}

public class SignInvoiceCommandHandler(
    IInvoiceRepository invoiceRepository,
    IDateTimeProvider dateTimeProvider,
    ICurrentUserService currentUserService)
    : IRequestHandler<SignInvoiceCommand, SignInvoiceResultDto>
{
    private const string SignedStatus = "Signed";

    public async Task<SignInvoiceResultDto> Handle(SignInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy hóa đơn.");

        if (invoice.Trangthai is not ("Draft" or "PendingSign"))
        {
            throw new InvalidOperationException($"Hóa đơn ở trạng thái '{invoice.Trangthai}' không thể ký số.");
        }

        var now = dateTimeProvider.UtcNow;
        var xmlDaky = GenerateMockXml(invoice, now);

        var history = new HoadonLichsu
        {
            Id = Guid.NewGuid(),
            Hoadonid = request.InvoiceId,
            Hanhdong = "Ký số hóa đơn",
            Trangthaicu = invoice.Trangthai,
            Trangthaimoi = SignedStatus,
            Thoigian = now,
            Nguoidungid = currentUserService.UserId
        };

        await invoiceRepository.UpdateSignedAsync(
            request.InvoiceId,
            xmlDaky,
            history,
            now,
            currentUserService.UserId,
            cancellationToken);

        return new SignInvoiceResultDto
        {
            Id = request.InvoiceId,
            Trangthai = SignedStatus,
            XmlDaKy = xmlDaky
        };
    }

    private static string GenerateMockXml(Hoadon invoice, DateTime signedAt)
    {
        return $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <HoaDon xmlns="http://lanhoadon.gdt.gov.vn/ns/invoice/1.0">
              <TTChung>
                <HoaDonId>{invoice.Id}</HoaDonId>
                <Ngaylap>{invoice.Ngaylap:yyyy-MM-dd}</Ngaylap>
                <TongTien>{invoice.Tongtien}</TongTien>
                <TienThue>{invoice.Tienthue}</TienThue>
                <TongThanhToan>{invoice.Tongthanhtoan}</TongThanhToan>
              </TTChung>
              <ChuKySo>
                <ThoiGianKy>{signedAt:o}</ThoiGianKy>
                <GiaTriChuKy>MOCK_SIGNATURE_{Guid.NewGuid():N}</GiaTriChuKy>
              </ChuKySo>
            </HoaDon>
            """;
    }
}
