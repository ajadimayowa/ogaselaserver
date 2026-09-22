namespace Ogasela.Api.Contracts.AdIntegrations;

public sealed record PromoteListingRequest(
    decimal BudgetKobo, int DurationDays, string? Audience, string? AdCopyTitle, string? AdCopyDescription);
