using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Templates.Commands.CancelTemplate;

public record CancelTemplateCommand(Guid Id) : IRequest<TemplateStatusHistoryDto>;

public class CancelTemplateCommandValidator : AbstractValidator<CancelTemplateCommand>
{
    public CancelTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

public class CancelTemplateCommandHandler(
    ITemplateRepository templateRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider)
    : IRequestHandler<CancelTemplateCommand, TemplateStatusHistoryDto>
{
    public async Task<TemplateStatusHistoryDto> Handle(CancelTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetCompanyTemplateByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn của công ty.");

        EnsureCanCancel(template.Trangthaiphathanh);

        var now = dateTimeProvider.UtcNow;

        // Cập nhật trạng thái sang 4 = Đã huỷ
        await templateRepository.UpdateCompanyTemplateStatusAsync(
            template.Id,
            trangthaiPhatHanh: 4,
            updatedAt: now,
            updatedBy: currentUserService.UserId,
            cancellationToken: cancellationToken);

        return new TemplateStatusHistoryDto
        {
            Trangthai = 4,
            Thoigian = now,
            Ghichu = "Mẫu phát hành đã được huỷ thành công."
        };
    }

    private static void EnsureCanCancel(short currentStatus)
    {
        // Chỉ cho phép huỷ khi trạng thái là:
        // 0 = Chưa phát hành
        // 2 = Đã chấp nhận (đã phát hành)
        // 3 = Từ chối
        if (currentStatus is not (0 or 2 or 3))
        {
            throw new InvalidOperationException(
                "Chỉ có thể huỷ mẫu phát hành khi trạng thái là Chưa phát hành, Đã chấp nhận hoặc Từ chối.");
        }
    }
}
