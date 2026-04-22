using System.Text;
using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Templates.Commands.NotifyTaxAuthority;

public record NotifyTaxAuthorityCommand(Guid Id) : IRequest<TemplateStatusHistoryDto>;

public class NotifyTaxAuthorityCommandValidator : AbstractValidator<NotifyTaxAuthorityCommand>
{
    public NotifyTaxAuthorityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class NotifyTaxAuthorityCommandHandler(
    ITemplateRepository templateRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<NotifyTaxAuthorityCommand, TemplateStatusHistoryDto>
{
    public async Task<TemplateStatusHistoryDto> Handle(NotifyTaxAuthorityCommand request, CancellationToken cancellationToken)
    {
        var template = await GetTemplateOrThrowAsync(request.Id, cancellationToken);
        EnsureValidStatusForNotify(template.Trangthaiphathanh);

        var taxPayloadXml = BuildTaxRegistrationXml(template);
        await MarkPendingAsync(template.Id, cancellationToken);

        var taxResponse = await SendToTaxAuthorityAsync(taxPayloadXml, cancellationToken);
        return await CompleteByTaxResponseAsync(template.Id, taxResponse, cancellationToken);
    }

    private async Task<Domain.Entities.Mauchocty> GetTemplateOrThrowAsync(Guid templateId, CancellationToken cancellationToken)
    {
        return await templateRepository.GetCompanyTemplateByIdAsync(templateId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn của công ty.");
    }

    private static void EnsureValidStatusForNotify(short currentStatus)
    {
        if (currentStatus is not 0 and not 3)
        {
            throw new InvalidOperationException("Chỉ có thể thông báo phát hành khi trạng thái là Chưa phát hành hoặc Từ chối.");
        }
    }

    private static string BuildTaxRegistrationXml(Domain.Entities.Mauchocty template)
    {
        return $"""
                <TaxTemplateRegistration>
                  <TemplateId>{template.Id}</TemplateId>
                  <CompanyId>{template.Donviid}</CompanyId>
                  <BaseTemplateId>{template.Maugocid}</BaseTemplateId>
                </TaxTemplateRegistration>
                """;
    }

    private async Task MarkPendingAsync(Guid templateId, CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        await templateRepository.UpdateCompanyTemplateStatusAsync(
            templateId,
            trangthaiPhatHanh: 1,
            updatedAt: now,
            updatedBy: currentUserService.UserId,
            cancellationToken: cancellationToken);
    }

    private static async Task<TaxNotifyResponse> SendToTaxAuthorityAsync(string xmlPayload, CancellationToken cancellationToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

        // Mock phản hồi từ TCT: nếu payload chứa Invalid thì trả về từ chối.
        var isRejected = xmlPayload.Contains("Invalid", StringComparison.OrdinalIgnoreCase);
        if (isRejected)
        {
            return new TaxNotifyResponse(false, "E_XML_INVALID", "TCT từ chối do cấu trúc XML không hợp lệ.");
        }

        return new TaxNotifyResponse(true, null, "Thông báo phát hành mẫu thành công.");
    }

    private async Task<TemplateStatusHistoryDto> CompleteByTaxResponseAsync(
        Guid templateId,
        TaxNotifyResponse taxResponse,
        CancellationToken cancellationToken)
    {
        var now = dateTimeProvider.UtcNow;
        if (taxResponse.Success)
        {
            await templateRepository.UpdateCompanyTemplateStatusAsync(
                templateId,
                trangthaiPhatHanh: 2,
                updatedAt: now,
                updatedBy: currentUserService.UserId,
                cancellationToken: cancellationToken);

            return new TemplateStatusHistoryDto
            {
                Trangthai = 2,
                Thoigian = now,
                Ghichu = taxResponse.Message
            };
        }

        await templateRepository.UpdateCompanyTemplateStatusAsync(
            templateId,
            trangthaiPhatHanh: 3,
            updatedAt: now,
            updatedBy: currentUserService.UserId,
            cancellationToken: cancellationToken);

        var detail = new StringBuilder("Mẫu bị từ chối.");
        if (!string.IsNullOrWhiteSpace(taxResponse.ErrorCode))
        {
            detail.Append($" Mã lỗi: {taxResponse.ErrorCode}.");
        }
        if (!string.IsNullOrWhiteSpace(taxResponse.Message))
        {
            detail.Append($" Chi tiết: {taxResponse.Message}");
        }

        throw new InvalidOperationException(detail.ToString());
    }

    private sealed record TaxNotifyResponse(bool Success, string? ErrorCode, string Message);
}
