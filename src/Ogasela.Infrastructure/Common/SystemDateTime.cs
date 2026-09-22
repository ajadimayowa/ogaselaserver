using Ogasela.Application.Common.Interfaces;

namespace Ogasela.Infrastructure.Common;

public sealed class SystemDateTime : IDateTime
{
    public DateTime UtcNow => DateTime.UtcNow;
}
