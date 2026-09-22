using Ogasela.Domain.Listings;

namespace Ogasela.Application.Ai.Interfaces;

/// <summary>
/// An always-on platform safety check run for every publish (see ListingPublishService),
/// regardless of the seller's plan tier - unlike the other three AI tools, this isn't a
/// seller-facing capability to gate, so it doesn't return a Result the caller can "fail" on.
/// </summary>
public interface IFraudRiskScorer
{
    Task<FraudRiskAssessment> ScoreAsync(Listing listing, CancellationToken cancellationToken);
}

/// <param name="RiskScore">0 (no signal) to 1 (highly suspicious).</param>
public sealed record FraudRiskAssessment(decimal RiskScore, string ReasonSummary);
