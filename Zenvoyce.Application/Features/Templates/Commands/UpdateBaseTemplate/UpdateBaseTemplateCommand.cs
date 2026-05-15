using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Templates.Commands.UpdateBaseTemplate;

public record UpdateBaseTemplateCommand(
    Guid Id,
    string Tenmau,
    string Loaihoadon,
    string Kyhieu,
    string HtmlContent,
    string? CssContent,
    string? Version) : IRequest<BaseTemplateDto>;

public class UpdateBaseTemplateCommandValidator : AbstractValidator<UpdateBaseTemplateCommand>
{
    public UpdateBaseTemplateCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Tenmau).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Loaihoadon).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Kyhieu).NotEmpty().MaximumLength(50);
        RuleFor(x => x.HtmlContent).NotEmpty().MinimumLength(10);
        RuleFor(x => x.Version).MaximumLength(20);
    }
}

public class UpdateBaseTemplateCommandHandler(
    ITemplateRepository templateRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<UpdateBaseTemplateCommand, BaseTemplateDto>
{
    public async Task<BaseTemplateDto> Handle(UpdateBaseTemplateCommand request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetBaseTemplateByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn gốc.");

        // if (await templateRepository.IsBaseTemplateInUseAsync(request.Id, cancellationToken))
        // {
        //     throw new InvalidOperationException("Không thể chỉnh sửa mẫu đã được đưa vào sử dụng.");
        // }

        var code = request.Kyhieu.Trim();
        if (await templateRepository.BaseTemplateCodeExistsAsync(code, request.Id, cancellationToken))
        {
            throw new InvalidOperationException("Ký hiệu mẫu hóa đơn đã tồn tại.");
        }

        template.Tenmau = request.Tenmau.Trim();
        template.Loaihoadon = request.Loaihoadon.Trim();
        template.Kyhieu = code;
        template.HtmlContent = request.HtmlContent;
        template.CssContent = request.CssContent;
        template.Version = string.IsNullOrWhiteSpace(request.Version) ? null : request.Version.Trim();
        template.UpdatedAt = dateTimeProvider.UtcNow;
        template.UpdatedBy = currentUserService.UserId;

        await templateRepository.UpdateBaseTemplateAsync(template, cancellationToken);

        return new BaseTemplateDto
        {
            Id = template.Id,
            Tenmau = template.Tenmau,
            Loaihoadon = template.Loaihoadon,
            Kyhieu = template.Kyhieu,
            HtmlContent = template.HtmlContent,
            CssContent = template.CssContent,
            Version = template.Version
        };
    }
}
