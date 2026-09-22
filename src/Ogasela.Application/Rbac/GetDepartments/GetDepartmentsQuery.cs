using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetDepartments;

public sealed record GetDepartmentsQuery : IRequest<Result<IReadOnlyList<DepartmentResponse>>>;
