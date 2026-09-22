using Microsoft.AspNetCore.Mvc;
using Ogasela.Domain.Listings;

namespace Ogasela.Api.Contracts.Ai;

public sealed record GenerateDescriptionRequest(Guid PromotionPlanId, string? Keywords, string? ImageRef);

public sealed record SuggestPriceRequest(Guid PromotionPlanId, Guid CategoryId, string Title, ListingCondition Condition);

/// <summary>Bound from multipart/form-data - a plain settable-property class binds IFormFile reliably, unlike a record.</summary>
public sealed class EnhanceImageRequest
{
    [FromForm(Name = "promotionPlanId")]
    public Guid PromotionPlanId { get; set; }

    [FromForm(Name = "image")]
    public IFormFile Image { get; set; } = null!;
}
