using Ogasela.Application.Common.Interfaces;

namespace Ogasela.UnitTests.TestSupport;

public sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; set; }
}
