using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Ogasela.Api.Common;
using Ogasela.Api.Contracts.Promotions;
using Ogasela.Application.Promotions.CreateCategory;
using Ogasela.Application.Promotions.GetCategories;
using Ogasela.Application.Promotions.UpdateCategory;

namespace Ogasela.Api.Controllers.Promotions;

/// <summary>Category browsing is public; creating or editing a category (including its optional cover image) is SuperAdmin-only.</summary>
[ApiController]
public sealed class CategoriesController : ControllerBase
{
    private readonly ISender _sender;

    public CategoriesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public - the full category tree, including inactive/non-free-eligible ones.</summary>
    [HttpGet("api/v1/categories")]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetCategoriesQuery(), cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>multipart/form-data; Image is optional. ParentCategoryId, if set, must reference an existing category.</summary>
    [HttpPost("api/v1/admin/categories")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> CreateCategory([FromForm] CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        await using var imageStream = request.Image?.OpenReadStream();

        var command = new CreateCategoryCommand(
            request.Name,
            request.ParentCategoryId,
            request.IsFreeEligible,
            request.AttributeSchemaVersion,
            imageStream,
            request.Image?.FileName,
            request.Image?.ContentType);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }

    /// <summary>multipart/form-data; a new Image replaces the existing one, otherwise it's left unchanged.</summary>
    [HttpPut("api/v1/admin/categories/{id:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<IActionResult> UpdateCategory(
        Guid id, [FromForm] UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await using var imageStream = request.Image?.OpenReadStream();

        var command = new UpdateCategoryCommand(
            id,
            request.Name,
            request.ParentCategoryId,
            request.IsFreeEligible,
            request.AttributeSchemaVersion,
            imageStream,
            request.Image?.FileName,
            request.Image?.ContentType);

        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult(this);
    }
}
