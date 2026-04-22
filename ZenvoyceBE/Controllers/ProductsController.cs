using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zenvoyce.Application.Features.Products.Commands.CreateProduct;
using Zenvoyce.Application.Features.Products.Commands.DeleteProduct;
using Zenvoyce.Application.Features.Products.Commands.UpdateProduct;
using Zenvoyce.Application.Features.Products.Queries.GetProductsByCompany;

namespace Zenvoyce.API.Controllers;

[ApiController]
[Authorize]
public class ProductsController(ISender mediator) : ControllerBase
{
    [HttpGet("api/companies/{donviId:guid}/products")]
    public async Task<IActionResult> GetByCompany(Guid donviId, [FromQuery] string? keyword)
    {
        var result = await mediator.Send(new GetProductsByCompanyQuery(donviId, keyword));
        return Ok(result);
    }

    [HttpPost("api/products")]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command)
    {
        var result = await mediator.Send(command);
        return CreatedAtAction(nameof(GetByCompany), new { donviId = result.Donviid }, result);
    }

    [HttpPut("api/products/{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductCommand command)
    {
        var result = await mediator.Send(command with { Id = id });
        return Ok(result);
    }

    [HttpDelete("api/products/{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await mediator.Send(new DeleteProductCommand(id));
        return Ok();
    }
}
