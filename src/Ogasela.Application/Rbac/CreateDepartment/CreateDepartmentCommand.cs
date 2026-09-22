using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.CreateDepartment;

public sealed record CreateDepartmentCommand(string Name, string? Description) : IRequest<Result<DepartmentResponse>>;
