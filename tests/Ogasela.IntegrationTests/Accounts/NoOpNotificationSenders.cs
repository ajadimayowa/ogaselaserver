using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;

namespace Ogasela.IntegrationTests.Accounts;

/// <summary>
/// Test doubles that stand in for the real Termii/Brevo senders so integration tests never
/// make real network calls to third-party providers (no cost, no flakiness from external
/// dependencies, and no need for real credentials in the test environment).
/// </summary>
public sealed class NoOpSmsSender : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
