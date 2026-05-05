using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Templates.Commands.CreateBaseTemplate;

public record CreateBaseTemplateCommand(
    string Tenmau,
    string Loaihoadon,
    string Kyhieu,
    string HtmlContent,
    string? CssContent,
    string? Version) : IRequest<BaseTemplateDto>;

public class CreateBaseTemplateCommandValidator : AbstractValidator<CreateBaseTemplateCommand>
{
    public CreateBaseTemplateCommandValidator()
    {
        RuleFor(x => x.Tenmau).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Loaihoadon).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Kyhieu).NotEmpty().MaximumLength(50);
        RuleFor(x => x.HtmlContent).NotEmpty().MinimumLength(10);
        RuleFor(x => x.Version).MaximumLength(20);
    }
}

public class CreateBaseTemplateCommandHandler(
    ITemplateRepository templateRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<CreateBaseTemplateCommand, BaseTemplateDto>
{
    public async Task<BaseTemplateDto> Handle(CreateBaseTemplateCommand request, CancellationToken cancellationToken)
    {
        var code = request.Kyhieu.Trim();
        if (await templateRepository.BaseTemplateCodeExistsAsync(code, null, cancellationToken))
        {
            throw new InvalidOperationException("Ký hiệu mẫu hóa đơn đã tồn tại.");
        }

        var now = dateTimeProvider.UtcNow;
        var template = new Mauhoadongoc
        {
            Id = Guid.NewGuid(),
            Tenmau = request.Tenmau.Trim(),
            Loaihoadon = request.Loaihoadon.Trim(),
            Kyhieu = code,
            HtmlContent = request.HtmlContent,
            CssContent = request.CssContent,
            Version = string.IsNullOrWhiteSpace(request.Version) ? null : request.Version.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        await templateRepository.AddBaseTemplateAsync(template, cancellationToken);

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
