namespace Ogasela.Domain.Staff;

/// <summary>
/// The staff-specific extension of a User (parallel to SellerProfile for Seller accounts).
/// Created with Status=PendingApproval and no usable password; ApproveStaffOnboardingCommand
/// generates one and emails it only once Status becomes Approved.
/// </summary>
public class StaffProfile
{
    private StaffProfile()
    {
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Guid UnitId { get; private set; }

    public Guid RoleId { get; private set; }

    public StaffOnboardingStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTime? DecidedAt { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static StaffProfile Create(Guid userId, Guid departmentId, Guid unitId, Guid roleId, Guid createdByUserId)
    {
        return new StaffProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DepartmentId = departmentId,
            UnitId = unitId,
            RoleId = roleId,
            Status = StaffOnboardingStatus.PendingApproval,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid reviewerId, DateTime decidedAt)
    {
        Status = StaffOnboardingStatus.Approved;
        DecidedByUserId = reviewerId;
        DecidedAt = decidedAt;
        RejectionReason = null;
    }

    public void Reject(Guid reviewerId, DateTime decidedAt, string reason)
    {
        Status = StaffOnboardingStatus.Rejected;
        DecidedByUserId = reviewerId;
        DecidedAt = decidedAt;
        RejectionReason = reason;
    }
}
