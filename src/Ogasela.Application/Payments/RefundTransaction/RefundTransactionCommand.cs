using MediatR;
using Ogasela.Application.Payments.GetTransactions;
using Ogasela.Shared;

namespace Ogasela.Application.Payments.RefundTransaction;

public sealed record RefundTransactionCommand(Guid TransactionId) : IRequest<Result<TransactionResponse>>;
