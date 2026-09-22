namespace Ogasela.Application.Ai;

public sealed class AiSettings
{
    public const string SectionName = "Ai";

    /// <summary>
    /// Which IListingCopyGenerator implementation is active - "Gemini" (default) or "Anthropic".
    /// Deliberately a plain string switch (see Infrastructure.DependencyInjection.AddAiTools),
    /// the same pattern Verification:Provider and Payments:Provider already use, so swapping (or
    /// adding a third) LLM never means changing anything above the interface.
    /// </summary>
    public string Provider { get; init; } = "Gemini";

    /// <summary>0-1. A listing whose IFraudRiskScorer score meets or exceeds this at publish time gets an automatic Report, but still publishes.</summary>
    public decimal FraudRiskThreshold { get; init; } = 0.5m;
}
