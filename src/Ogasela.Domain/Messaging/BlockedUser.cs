namespace Ogasela.Domain.Messaging;

/// <summary>
/// Records that <see cref="BlockerId"/> has blocked <see cref="BlockedId"/>. Not part of the
/// phase's literal entity list, but required for BlockUserCommand to persist anything and for
/// SendMessageCommand to have something to check.
/// </summary>
public class BlockedUser
{
    private BlockedUser()
    {
    }

    public Guid Id { get; private set; }

    public Guid BlockerId { get; private set; }

    public Guid BlockedId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static BlockedUser Create(Guid blockerId, Guid blockedId, DateTime now)
    {
        return new BlockedUser
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedId = blockedId,
            CreatedAt = now
        };
    }
}
