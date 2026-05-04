using MediatR;
using Zenvoyce.Application.Abstractions.Persistence;
using Zenvoyce.Application.Features.Templates.DTOs;

namespace Zenvoyce.Application.Features.Templates.Queries.GetBaseTemplates;

public record GetBaseTemplatesQuery : IRequest<IReadOnlyCollection<BaseTemplateDto>>;

public class GetBaseTemplatesQueryHandler(ITemplateRepository templateRepository)
    : IRequestHandler<GetBaseTemplatesQuery, IReadOnlyCollection<BaseTemplateDto>>
{
    public async Task<IReadOnlyCollection<BaseTemplateDto>> Handle(GetBaseTemplatesQuery request, CancellationToken cancellationToken)
    {
        var templates = await templateRepository.GetBaseTemplatesAsync(cancellationToken);
        return templates
            .Select(x => new BaseTemplateDto
            {
                Id = x.Id,
                Tenmau = x.Tenmau,
                Loaihoadon = x.Loaihoadon,
                Kyhieu = x.Kyhieu,
                Cautrucxml = x.Cautrucxml
            })
            .ToArray();
    }
}
