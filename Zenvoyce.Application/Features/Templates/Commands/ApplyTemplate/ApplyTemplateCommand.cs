using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;
using Zenvoyce.Domain.Entities;
using Zenvoyce.Domain.Interfaces;

namespace Zenvoyce.Application.Features.Templates.Commands.ApplyTemplate;

public record ApplyTemplateCommand(
    Guid Maugocid,
    Guid Donviid,
    string? Css,
    string? Header,
    bool Lamaumacdinh,
    IReadOnlyCollection<ApplyTemplateMetadataItem> Metadata) : IRequest<CompanyTemplateDto>;

public record ApplyTemplateMetadataItem(
    string? Tentruong,
    string? Vitrinam,
    string? Font,
    string? Canle);

public class ApplyTemplateCommandValidator : AbstractValidator<ApplyTemplateCommand>
{
    public ApplyTemplateCommandValidator()
    {
        RuleFor(x => x.Maugocid).NotEmpty();
        RuleFor(x => x.Donviid).NotEmpty();
        RuleFor(x => x.Metadata).NotNull();

        RuleForEach(x => x.Metadata).SetValidator(new ApplyTemplateMetadataItemValidator());
    }
}

public class ApplyTemplateMetadataItemValidator : AbstractValidator<ApplyTemplateMetadataItem>
{
    public ApplyTemplateMetadataItemValidator()
    {
        RuleFor(x => x.Tentruong).MaximumLength(100);
        RuleFor(x => x.Vitrinam).MaximumLength(50);
        RuleFor(x => x.Font).MaximumLength(50);
        RuleFor(x => x.Canle).MaximumLength(20);
    }
}

public class ApplyTemplateCommandHandler(
    ITemplateRepository templateRepository,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider) : IRequestHandler<ApplyTemplateCommand, CompanyTemplateDto>
{
    public async Task<CompanyTemplateDto> Handle(ApplyTemplateCommand request, CancellationToken cancellationToken)
    {
        if (!await templateRepository.BaseTemplateExistsAsync(request.Maugocid, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn gốc.");
        }

        if (!await templateRepository.CompanyExistsAsync(request.Donviid, cancellationToken))
        {
            throw new KeyNotFoundException("Không tìm thấy đơn vị áp dụng.");
        }

        var baseTemplate = await templateRepository.GetBaseTemplateByIdAsync(request.Maugocid, cancellationToken);

        var now = dateTimeProvider.UtcNow;
        var companyTemplateId = Guid.NewGuid();
        var companyTemplate = new Mauchocty
        {
            Id = companyTemplateId,
            Maugocid = request.Maugocid,
            Donviid = request.Donviid,
            Css = request.Css?.Trim(),
            Header = request.Header?.Trim(),
            Lamaumacdinh = request.Lamaumacdinh,
            Trangthaiphathanh = 0,
            Ngaykichhoat = now,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = currentUserService.UserId,
            UpdatedBy = currentUserService.UserId,
            IsDeleted = false
        };

        var metadata = request.Metadata
            .Select(x => new Thongtinhdmau
            {
                Id = Guid.NewGuid(),
                Mauctyid = companyTemplateId,
                Tentruong = x.Tentruong?.Trim(),
                Vitrinam = x.Vitrinam?.Trim(),
                Font = x.Font?.Trim(),
                Canle = x.Canle?.Trim()
            })
            .ToArray();

        await templateRepository.ApplyTemplateAsync(companyTemplate, metadata, request.Lamaumacdinh, cancellationToken);

        return new CompanyTemplateDto
        {
            Id = companyTemplate.Id,
            Maugocid = companyTemplate.Maugocid,
            Donviid = companyTemplate.Donviid,
            Tenmaugoc = baseTemplate?.Tenmau,
            Kyhieu = baseTemplate?.Kyhieu,
            Loaihoadon = baseTemplate?.Loaihoadon,
            Css = companyTemplate.Css,
            Header = companyTemplate.Header,
            Trangthaiphathanh = companyTemplate.Trangthaiphathanh,
            Lamaumacdinh = companyTemplate.Lamaumacdinh,
            Ngaykichhoat = companyTemplate.Ngaykichhoat,
            Metadata = metadata.Select(x => new TemplateMetadataDto
            {
                Tentruong = x.Tentruong,
                Vitrinam = x.Vitrinam,
                Font = x.Font,
                Canle = x.Canle
            }).ToArray()
        };
    }
}
