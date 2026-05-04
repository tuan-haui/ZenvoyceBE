using FluentValidation;
using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;

namespace Zenvoyce.Application.Features.Templates.Queries.GetCompanyTemplates;

public record GetCompanyTemplatesQuery(
    Guid DonviId,
    string? KyhieuMau,
    string? LoaiHoadon,
    short? TrangthaiPhatHanh) : IRequest<IReadOnlyCollection<CompanyTemplateDto>>;

public class GetCompanyTemplatesQueryValidator : AbstractValidator<GetCompanyTemplatesQuery>
{
    public GetCompanyTemplatesQueryValidator()
    {
        RuleFor(x => x.DonviId).NotEmpty();
        RuleFor(x => x.TrangthaiPhatHanh)
            .InclusiveBetween((short)0, (short)3)
            .When(x => x.TrangthaiPhatHanh.HasValue);
    }
}

public class GetCompanyTemplatesQueryHandler(ITemplateRepository templateRepository)
    : IRequestHandler<GetCompanyTemplatesQuery, IReadOnlyCollection<CompanyTemplateDto>>
{
    public async Task<IReadOnlyCollection<CompanyTemplateDto>> Handle(GetCompanyTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await templateRepository.GetCompanyTemplatesAsync(
            request.DonviId,
            request.KyhieuMau,
            request.LoaiHoadon,
            request.TrangthaiPhatHanh,
            cancellationToken);

        return templates.Select(x => new CompanyTemplateDto
        {
            Id = x.Id,
            Maugocid = x.Maugocid,
            Donviid = x.Donviid,
            Tenmaugoc = x.Tenmau,
            Kyhieu = x.Kyhieu,
            Loaihoadon = x.Loaihoadon,
            Css = x.Css,
            Header = x.Header,
            Trangthaiphathanh = x.Trangthaiphathanh,
            Lamaumacdinh = x.Lamaumacdinh,
            Ngaykichhoat = x.Ngaykichhoat,
            Metadata = x.Metadata.Select(m => new TemplateMetadataDto
            {
                Tentruong = m.Tentruong,
                Vitrinam = m.Vitrinam,
                Font = m.Font,
                Canle = m.Canle
            }).ToArray(),
            LichsuTrangthai =
            [
                new TemplateStatusHistoryDto
                {
                    Trangthai = x.Trangthaiphathanh,
                    Thoigian = x.UpdatedAt,
                    Ghichu = "Trạng thái hiện tại của mẫu hóa đơn."
                }
            ]
        }).ToArray();
    }
}
