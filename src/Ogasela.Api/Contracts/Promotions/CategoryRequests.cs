using Microsoft.AspNetCore.Mvc;

namespace Ogasela.Api.Contracts.Promotions;

/// <summary>Bound from multipart/form-data - a plain settable-property class binds an optional IFormFile reliably, unlike a record.</summary>
public sealed class CreateCategoryRequest
{
    [FromForm(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [FromForm(Name = "parentCategoryId")]
    public Guid? ParentCategoryId { get; set; }

    [FromForm(Name = "isFreeEligible")]
    public bool IsFreeEligible { get; set; } = true;

    [FromForm(Name = "attributeSchemaVersion")]
    public int AttributeSchemaVersion { get; set; } = 1;

    /// <summary>The category image when creating a top-level category, or the subcategory image when ParentCategoryId is set. Optional.</summary>
    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }
}

public sealed class UpdateCategoryRequest
{
    [FromForm(Name = "name")]
    public string Name { get; set; } = string.Empty;

    [FromForm(Name = "parentCategoryId")]
    public Guid? ParentCategoryId { get; set; }

    [FromForm(Name = "isFreeEligible")]
    public bool IsFreeEligible { get; set; }

    [FromForm(Name = "attributeSchemaVersion")]
    public int AttributeSchemaVersion { get; set; }

    [FromForm(Name = "image")]
    public IFormFile? Image { get; set; }
}
