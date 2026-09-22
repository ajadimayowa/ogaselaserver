using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Staff.ApproveStaffOnboarding;

public sealed record ApproveStaffOnboardingCommand(Guid StaffProfileId) : IRequest<Result>;
