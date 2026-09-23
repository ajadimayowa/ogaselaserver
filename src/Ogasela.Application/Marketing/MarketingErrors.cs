using Ogasela.Shared;

namespace Ogasela.Application.Marketing;

public static class MarketingErrors
{
    public static readonly Error TurnstileVerificationFailed =
        new("Turnstile.VerificationFailed", "We couldn't verify you're human. Please try again.");
}
