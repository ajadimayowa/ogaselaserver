using MediatR;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.ConnectAdAccount;

public sealed record ConnectAdAccountCommand(AdPlatform Platform) : IRequest<Result<ConnectAdAccountResponse>>;

public sealed record ConnectAdAccountResponse(string AuthorizationUrl);
