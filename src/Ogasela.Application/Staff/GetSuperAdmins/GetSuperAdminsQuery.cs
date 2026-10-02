using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.GetSuperAdmins;

/// <summary>
/// Every SuperAdmin account. These are provisioned by SuperAdminSeeder rather than staff
/// onboarding, so they have no StaffProfile and never appear in GetStaffListQuery.
/// </summary>
public sealed record GetSuperAdminsQuery : IRequest<Result<IReadOnlyList<SuperAdminResponse>>>;

public sealed record SuperAdminResponse(Guid Id, string? Name, string? Email, string? Phone, DateTime CreatedAt);
