using MediatR;
using Ogasela.Domain.Payments;
using Ogasela.Shared;

namespace Ogasela.Application.Payments.GetTransactions;

public sealed record GetTransactionsQuery(int Page = 1, int PageSize = 20) : IRequest<Result<PagedTransactionsResponse>>;

public sealed record TransactionResponse(
    Guid Id,
    TransactionType Type,
    decimal AmountKobo,
    string? GatewayReference,
    TransactionStatus Status,
    Guid? RelatedListingId,
    DateTime CreatedAt);

public sealed record PagedTransactionsResponse(
    IReadOnlyList<TransactionResponse> Items, int Page, int PageSize, int TotalCount);
