namespace Ogasela.Infrastructure.Staff;

public sealed class StaffKycSettings
{
    public const string SectionName = "StaffKyc";

    /// <summary>Key prefix under the bucket that staff KYC document files are written to.</summary>
    public string UploadPrefix { get; init; } = "staff-kyc";
}
