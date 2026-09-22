using Ogasela.Application.Common.Interfaces;

namespace Ogasela.UnitTests.TestSupport;

public sealed class FakeDateTime : IDateTime
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
