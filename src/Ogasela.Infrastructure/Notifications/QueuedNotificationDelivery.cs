using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;

namespace Ogasela.Infrastructure.Notifications;

/// <summary>
/// In-process queue between the request path and the real SMS/email providers. Sending is slow
/// (a Brevo SMTP connect + STARTTLS alone measured ~17s, a Termii call ~4s), and every caller
/// already treats delivery as best-effort, so handlers enqueue and return immediately while
/// <see cref="NotificationDeliveryWorker"/> does the sending. Trade-off: a message still queued
/// when the process restarts is lost - for OTPs the user just asks for a new code.
/// </summary>
public sealed class NotificationQueue
{
    private readonly Channel<OutboundMessage> _channel = Channel.CreateUnbounded<OutboundMessage>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(OutboundMessage message) => _channel.Writer.TryWrite(message);

    public ChannelReader<OutboundMessage> Reader => _channel.Reader;
}

public abstract record OutboundMessage;

public sealed record OutboundSms(string PhoneNumber, string Message) : OutboundMessage;

public sealed record OutboundEmail(EmailMessage Message) : OutboundMessage;

/// <summary>The ISmsSender handlers get: queues the SMS and returns at once.</summary>
public sealed class QueuedSmsSender : ISmsSender
{
    private readonly NotificationQueue _queue;

    public QueuedSmsSender(NotificationQueue queue)
    {
        _queue = queue;
    }

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        _queue.Enqueue(new OutboundSms(phoneNumber, message));
        return Task.CompletedTask;
    }
}

/// <summary>The IEmailSender handlers get: queues the email and returns at once.</summary>
public sealed class QueuedEmailSender : IEmailSender
{
    private readonly NotificationQueue _queue;

    public QueuedEmailSender(NotificationQueue queue)
    {
        _queue = queue;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _queue.Enqueue(new OutboundEmail(message));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Delivers queued messages through the real providers (TermiiSmsSender, SmtpEmailSender), several
/// at a time so a slow email never holds up someone else's login code. Failures are logged - the
/// same outcome callers already had with their try/catch around the send.
/// </summary>
public sealed class NotificationDeliveryWorker : BackgroundService
{
    private const int MaxConcurrentSends = 8;

    private readonly NotificationQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDeliveryWorker> _logger;
    private readonly SemaphoreSlim _slots = new(MaxConcurrentSends);

    public NotificationDeliveryWorker(
        NotificationQueue queue, IServiceScopeFactory scopeFactory, ILogger<NotificationDeliveryWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var inFlight = new List<Task>();
        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken))
            {
                await _slots.WaitAsync(stoppingToken);
                inFlight.RemoveAll(t => t.IsCompleted);
                inFlight.Add(DeliverAsync(message));
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down - let the sends already under way finish below.
        }

        await Task.WhenAll(inFlight);
    }

    private async Task DeliverAsync(OutboundMessage message)
    {
        try
        {
            // A real send shouldn't be cut off by shutdown; the providers' own timeouts bound it.
            using var scope = _scopeFactory.CreateScope();
            switch (message)
            {
                case OutboundSms sms:
                    await scope.ServiceProvider.GetRequiredService<TermiiSmsSender>()
                        .SendAsync(sms.PhoneNumber, sms.Message, CancellationToken.None);
                    break;
                case OutboundEmail email:
                    await scope.ServiceProvider.GetRequiredService<SmtpEmailSender>()
                        .SendAsync(email.Message, CancellationToken.None);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver queued {MessageType}", message.GetType().Name);
        }
        finally
        {
            _slots.Release();
        }
    }
}
