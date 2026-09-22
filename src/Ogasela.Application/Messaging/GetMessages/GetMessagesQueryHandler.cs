using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Messaging.SendMessage;
using Ogasela.Shared;

namespace Ogasela.Application.Messaging.GetMessages;

public sealed class GetMessagesQueryHandler : IRequestHandler<GetMessagesQuery, Result<PagedMessagesResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTime _dateTime;

    public GetMessagesQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUser, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _dateTime = dateTime;
    }

    public async Task<Result<PagedMessagesResponse>> Handle(GetMessagesQuery request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId!.Value;

        var conversation = await _dbContext.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, cancellationToken);

        if (conversation is null)
        {
            return Result.Failure<PagedMessagesResponse>(MessagingErrors.ConversationNotFound);
        }

        if (!conversation.HasParticipant(userId))
        {
            return Result.Failure<PagedMessagesResponse>(MessagingErrors.NotParticipant);
        }

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 200);

        var query = _dbContext.Messages.Where(m => m.ConversationId == request.ConversationId);

        var totalCount = await query.CountAsync(cancellationToken);

        // Page 1 = the most recent window; within that window, items are then put back into
        // chronological (oldest-first / "newest last") order for display.
        var page1Newest = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        page1Newest.Reverse();

        // Viewing the conversation clears unread indicators for messages the other party sent.
        var unreadFromOtherParty = await _dbContext.Messages
            .Where(m => m.ConversationId == request.ConversationId && m.SenderId != userId && m.ReadAt == null)
            .ToListAsync(cancellationToken);

        if (unreadFromOtherParty.Count > 0)
        {
            var now = _dateTime.UtcNow;
            foreach (var message in unreadFromOtherParty)
            {
                message.MarkRead(now);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var items = page1Newest
            .Select(m => new MessageResponse(m.Id, m.ConversationId, m.SenderId, m.Content, m.ImageUrl, m.SentAt, m.ReadAt))
            .ToList();

        return Result.Success(new PagedMessagesResponse(items, page, pageSize, totalCount));
    }
}
