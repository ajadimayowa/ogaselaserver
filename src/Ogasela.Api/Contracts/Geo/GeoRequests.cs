namespace Ogasela.Api.Contracts.Geo;

public sealed record CreateStateRequest(string Name, string Code);

public sealed record UpdateStateRequest(string Name, string Code);

public sealed record CreateCityRequest(Guid StateId, string Name);

public sealed record UpdateCityRequest(string Name);
