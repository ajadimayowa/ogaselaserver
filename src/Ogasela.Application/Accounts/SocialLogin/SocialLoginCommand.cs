using MediatR;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;

namespace Ogasela.Application.Accounts.SocialLogin;

/// <summary>
/// Signs in - or signs up - with Google, Apple or Facebook in one step. Token is the provider's ID
/// token (Google/Apple) or access token (Facebook). Name is optional and only used for a new
/// account: Apple shares the user's name with the app only on the very first sign-in, never inside
/// the token. AccountType applies to new accounts only (Buyer or Seller).
/// </summary>
public sealed record SocialLoginCommand(
    ExternalLoginProvider Provider,
    string Token,
    string? Name,
    UserRole AccountType) : IRequest<Result<SocialLoginResponse>>;

/// <summary>The same token pair as every other sign-in, plus whether this call created the account.</summary>
public sealed record SocialLoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    bool IsNewUser);
