namespace Ogasela.Domain.Messaging;

public class Message
{
    private Message()
    {
    }

    public Guid Id { get; private set; }

    public Guid ConversationId { get; private set; }

    public Guid SenderId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public string? ImageUrl { get; private set; }

    public DateTime SentAt { get; private set; }

    public DateTime? ReadAt { get; private set; }

    public static Message Send(Guid conversationId, Guid senderId, string content, string? imageUrl, DateTime now)
    {
        return new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            SenderId = senderId,
            Content = content,
            ImageUrl = imageUrl,
            SentAt = now
        };
    }

    public void MarkRead(DateTime now)
    {
        ReadAt ??= now;
    }
}
