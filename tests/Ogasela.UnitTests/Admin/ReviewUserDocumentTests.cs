using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Admin.Users;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Admin;

public class ReviewUserDocumentTests
{
    private sealed class NoOpAuditLogger : IAuditLogger
    {
        public Task LogAsync(Guid actorId, string action, string targetType, Guid targetId,
            string? beforeJson, string? afterJson, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static async Task<(AdminUserCommandHandlers Handler, TestApplicationDbContext Db, UserDocument Document)> SetupAsync()
    {
        var db = TestApplicationDbContext.Create();
        var clock = new FakeDateTime();
        var document = UserDocument.Create(Guid.NewGuid(), UserDocumentType.IdCard, IdDocumentType.NationalIdCard,
            "A1234567", "user-documents/a.jpg", "a.jpg", "image/jpeg", clock.UtcNow);
        db.UserDocuments.Add(document);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new AdminUserCommandHandlers(
            db, new FakeCurrentUserService { UserId = Guid.NewGuid() }, new NoOpAuditLogger(), clock);
        return (handler, db, document);
    }

    [Fact]
    public async Task Reject_StoresTheReasonAndNotifiesTheUser()
    {
        var (handler, db, document) = await SetupAsync();

        var result = await handler.Handle(
            new ReviewUserDocumentCommand(document.UserId, document.Id, false, "The photo is blurry."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.UserDocuments.SingleAsync();
        reloaded.Status.Should().Be(UserDocumentStatus.Rejected);
        reloaded.ReviewNote.Should().Be("The photo is blurry.");
        var notification = await db.InboxNotifications.SingleAsync();
        notification.UserId.Should().Be(document.UserId);
        notification.Type.Should().Be("DocumentRejected");
        notification.Body.Should().Contain("The photo is blurry.");
    }

    [Fact]
    public async Task Approve_ThenReviewingAgain_IsRefused()
    {
        var (handler, db, document) = await SetupAsync();

        (await handler.Handle(new ReviewUserDocumentCommand(document.UserId, document.Id, true, null), CancellationToken.None))
            .IsSuccess.Should().BeTrue();
        var again = await handler.Handle(new ReviewUserDocumentCommand(document.UserId, document.Id, false, "x"), CancellationToken.None);

        (await db.UserDocuments.SingleAsync()).Status.Should().Be(UserDocumentStatus.Approved);
        again.Error.Code.Should().Be("AdminUsers.DocumentAlreadyReviewed");
    }
}
