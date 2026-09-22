using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Rbac.GetUnits;

/// <summary>DepartmentId is optional - omitted, this returns every unit across every department.</summary>
public sealed record GetUnitsQuery(Guid? DepartmentId) : IRequest<Result<IReadOnlyList<OrgUnitResponse>>>;
