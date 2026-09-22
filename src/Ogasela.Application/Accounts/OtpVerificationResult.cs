namespace Ogasela.Application.Accounts;

public enum OtpVerificationResult
{
    Success,
    InvalidCode,
    Expired,
    RateLimited
}
