using MediatR;

namespace Ogasela.UnitTests.TestSupport;

/// <summary>A no-op IPublisher for handler unit tests that publish a domain event but don't need to assert on it.</summary>
public sealed class FakePublisher : IPublisher
{
    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}
