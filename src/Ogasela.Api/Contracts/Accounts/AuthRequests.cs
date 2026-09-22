using Ogasela.Domain.Accounts;

namespace Ogasela.Api.Contracts.Accounts;

public sealed record RegisterRequest(
    string? Phone,
    string? Email,
    string Password,
    UserRole AccountType,
    string? BusinessName,
    string? RcNumber,
    string? Nin);

public sealed record RequestOtpRequest(string Phone);

public sealed record VerifyOtpRequest(string Phone, string Code);

public sealed record ConfirmPasswordResetRequest(string Phone, string Code, string NewPassword);

public sealed record LoginRequest(string? Phone, string? Email, string Password);

public sealed record AdminLoginRequest(string Email, string Password);

public sealed record VerifyAdminLoginOtpRequest(string Email, string Code);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record UpdateProfileRequest(string? Phone, string? Email, string? BusinessName);

public sealed record SetPushTokenRequest(string? Token);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
