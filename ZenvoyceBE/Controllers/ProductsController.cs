using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Common.Models;
using Zenvoyce.Application.Features.Products.Commands.CreateProduct;
using Zenvoyce.Application.Features.Products.Commands.DeleteProduct;
using Zenvoyce.Application.Features.Products.Commands.UpdateProduct;
using Zenvoyce.Application.Features.Products.DTOs;
using Zenvoyce.Application.Features.Products.Queries.GetProductsByCompany;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ProductsController(ISender mediator) : ControllerBase
{
    [HttpGet("{donviId:guid}/products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ProductDto>>>> GetByCompany(Guid donviId,
        [FromQuery] string? keyword)
    {
        var result = await mediator.Send(new GetProductsByCompanyQuery(donviId, keyword));
        return Ok(ApiResponse<IReadOnlyCollection<ProductDto>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Create([FromBody] CreateProductCommand command)
    {
        var result = await mediator.Send(command);
        return Ok(ApiResponse<ProductDto>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ProductDto>>> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(ApiResponse<ProductDto>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id)
    {
        await mediator.Send(new DeleteProductCommand(id));
        return Ok(ApiResponse<object?>.Ok(null));
    }
}
