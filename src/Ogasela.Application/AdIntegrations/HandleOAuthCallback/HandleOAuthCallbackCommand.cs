using MediatR;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.HandleOAuthCallback;

/// <summary>
/// State is the sellerId the platform's redirect round-trips back (see IAdPlatformClient.GetOAuthUrlAsync)
/// - this endpoint is [AllowAnonymous] since the platform's own redirect carries no bearer token,
/// so State is how the callback is tied back to the seller who started the flow.
/// </summary>
public sealed record HandleOAuthCallbackCommand(AdPlatform Platform, string Code, string State) : IRequest<Result>;
