using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Admin.EraseUserData;

/// <summary>An NDPA data-subject deletion request, orchestrating every module that holds this user's identifying data.</summary>
public sealed record EraseUserDataCommand(Guid UserId) : IRequest<Result<EraseUserDataReceipt>>;

public sealed record EraseUserDataReceipt(
    Guid UserId,
    int BiometricVerificationsErased,
    int AdAccountConnectionsRevoked,
    int RefreshTokensRevoked,
    bool SellerProfileAnonymized,
    DateTime ErasedAt);
