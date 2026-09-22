using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.RejectStaffOnboarding;

public sealed record RejectStaffOnboardingCommand(Guid StaffProfileId, string Reason) : IRequest<Result>;
