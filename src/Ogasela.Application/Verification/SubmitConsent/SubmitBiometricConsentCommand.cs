using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Verification.SubmitConsent;

public sealed record SubmitBiometricConsentCommand(string IpAddress) : IRequest<Result<SubmitBiometricConsentResponse>>;
