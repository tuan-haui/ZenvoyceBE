using System.Xml.Linq;
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
    string Cautrucxml) : IRequest<BaseTemplateDto>;

public class CreateBaseTemplateCommandValidator : AbstractValidator<CreateBaseTemplateCommand>
{
    public CreateBaseTemplateCommandValidator()
    {
        RuleFor(x => x.Tenmau).NotEmpty().MaximumLength(255);
        RuleFor(x => x.Loaihoadon).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Kyhieu).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Cautrucxml).NotEmpty();
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

        ValidateXml(request.Cautrucxml);

        var now = dateTimeProvider.UtcNow;
        var template = new Mauhoadongoc
        {
            Id = Guid.NewGuid(),
            Tenmau = request.Tenmau.Trim(),
            Loaihoadon = request.Loaihoadon.Trim(),
            Kyhieu = code,
            Cautrucxml = request.Cautrucxml.Trim(),
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
            Cautrucxml = template.Cautrucxml
        };
    }

    private static void ValidateXml(string xmlContent)
    {
        try
        {
            _ = XDocument.Parse(xmlContent, LoadOptions.None);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Cấu trúc XML không hợp lệ.");
        }
    }
}
