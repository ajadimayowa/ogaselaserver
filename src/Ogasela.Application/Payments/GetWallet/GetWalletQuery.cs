using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Payments.GetWallet;

public sealed record GetWalletQuery : IRequest<Result<WalletResponse>>;

public sealed record WalletResponse(Guid SellerId, decimal BalanceKobo, DateTime UpdatedAt);
