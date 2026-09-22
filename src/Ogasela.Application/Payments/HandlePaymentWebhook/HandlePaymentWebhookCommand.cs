using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Payments.HandlePaymentWebhook;

public sealed record HandlePaymentWebhookCommand(
    string Provider, string RawBody, string? SignatureHeaderValue) : IRequest<Result>;
