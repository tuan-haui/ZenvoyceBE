using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;

namespace Zenvoyce.Application.Features.Templates.Queries.GetBaseTemplateById;

public record GetBaseTemplateByIdQuery(Guid Id) : IRequest<BaseTemplateDto>;

public class GetBaseTemplateByIdQueryHandler(ITemplateRepository templateRepository)
    : IRequestHandler<GetBaseTemplateByIdQuery, BaseTemplateDto>
{
    public async Task<BaseTemplateDto> Handle(GetBaseTemplateByIdQuery request, CancellationToken cancellationToken)
    {
        var template = await templateRepository.GetBaseTemplateByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy mẫu hóa đơn gốc.");

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
