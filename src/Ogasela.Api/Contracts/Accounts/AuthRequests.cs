using Ogasela.Application.Accounts.RequestPasswordReset;
using Ogasela.Domain.Accounts;

namespace Ogasela.Api.Contracts.Accounts;

public sealed record RegisterRequest(
    string? Phone,
    string? Email,
    string Password,
    UserRole AccountType,
    string? BusinessName,
    string? RcNumber,
    string? Nin,
    string? Name = null);

public sealed record RequestOtpRequest(string Phone);

public sealed record VerifyOtpRequest(string Phone, string Code);

public sealed record RequestPasswordResetRequest(PasswordResetChannel Channel, string? Email, string? Phone);

/// <summary>Exactly one of Phone/Email - whichever the code was sent to.</summary>
public sealed record ConfirmPasswordResetRequest(string? Phone, string Code, string NewPassword, string? Email = null);

public sealed record LoginRequest(string? Phone, string? Email, string Password);

public sealed record VerifyLoginOtpRequest(string? Phone, string? Email, string Code);

public sealed record AdminLoginRequest(string Email, string Password);

public sealed record VerifyAdminLoginOtpRequest(string Email, string Code);

public sealed record RequestAdminPasswordResetRequest(string Email);

public sealed record ConfirmAdminPasswordResetRequest(string Email, string Code, string NewPassword);

/// <summary>Token: Google/Apple ID token, or Facebook access token. AccountType applies only when this creates the account (defaults to Seller, like the app's sign-up).</summary>
public sealed record SocialLoginRequest(ExternalLoginProvider Provider, string Token, string? Name, UserRole AccountType = UserRole.Seller);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record UpdateProfileRequest(string Name);

/// <summary>Type is Phone or Email; Value is the new phone number or email address.</summary>
public sealed record RequestContactChangeCodeRequest(ProfileChangeType Type, string Value);

public sealed record SubmitContactChangeRequest(ProfileChangeType Type, string Value, string Code);

/// <summary>An empty or null StoreAddress clears it once approved.</summary>
public sealed record SubmitBusinessInfoChangeRequest(string BusinessName, string? StoreAddress);

/// <summary>Bound from multipart/form-data. IdType and IdNumber are required for an IdCard, empty for every other type.</summary>
public sealed class UploadUserDocumentRequest
{
    [Microsoft.AspNetCore.Mvc.FromForm(Name = "type")]
    public UserDocumentType Type { get; set; }

    [Microsoft.AspNetCore.Mvc.FromForm(Name = "idType")]
    public IdDocumentType? IdType { get; set; }

    [Microsoft.AspNetCore.Mvc.FromForm(Name = "idNumber")]
    public string? IdNumber { get; set; }

    [Microsoft.AspNetCore.Mvc.FromForm(Name = "file")]
    public Microsoft.AspNetCore.Http.IFormFile? File { get; set; }
}

public sealed record SetPushTokenRequest(string? Token);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
