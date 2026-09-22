namespace Ogasela.Domain.Accounts;

public enum UserRole
{
    Buyer,
    Seller,
    SupportAgent,
    Moderator,
    FinanceAdmin,
    SuperAdmin,

    /// <summary>
    /// Any account onboarded through the Staff/RBAC system (departments, units, dynamic Role
    /// with permissions). Carries a linked StaffProfile with the actual RoleType/permissions -
    /// this enum value on its own only marks "this is an internal staff account requiring MFA
    /// login", parallel to but independent of the fixed Moderator/FinanceAdmin/SuperAdmin values.
    /// </summary>
    Staff
}
