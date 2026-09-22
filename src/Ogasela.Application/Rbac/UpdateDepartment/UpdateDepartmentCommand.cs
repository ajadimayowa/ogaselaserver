using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.UpdateDepartment;

public sealed record UpdateDepartmentCommand(Guid Id, string Name, string? Description) : IRequest<Result<DepartmentResponse>>;
