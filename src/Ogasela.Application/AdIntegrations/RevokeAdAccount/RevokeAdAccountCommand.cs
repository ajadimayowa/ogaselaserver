using MediatR;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.RevokeAdAccount;

public sealed record RevokeAdAccountCommand(AdPlatform Platform) : IRequest<Result>;
