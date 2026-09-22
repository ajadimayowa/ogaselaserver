using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Payments.FundWallet;

public sealed record FundWalletCommand(decimal AmountKobo) : IRequest<Result<FundWalletResponse>>;

public sealed record FundWalletResponse(Guid TransactionId, string GatewayReference, string RedirectUrl);
