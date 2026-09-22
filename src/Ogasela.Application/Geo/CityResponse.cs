namespace Ogasela.Application.Geo;

public sealed record CityResponse(Guid Id, Guid StateId, string Name, DateTime CreatedAt);
