using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Admin.Users;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Admin;

public class SetSellerVerificationTests
{
    private sealed class NoOpAuditLogger : IAuditLogger
    {
        public Task LogAsync(Guid actorId, string action, string targetType, Guid targetId,
            string? beforeJson, string? afterJson, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static async Task<(AdminUserCommandHandlers Handler, TestApplicationDbContext Db, SellerProfile Seller)> SetupAsync()
    {
        var db = TestApplicationDbContext.Create();
        var user = User.Create("08031234567", "ada@example.com", "hash", UserRole.Seller, "Ada");
        user.SetProfilePhoto("profile-photos/ada.jpg");
        db.Users.Add(user);
        var seller = SellerProfile.Create(user.Id, "Ada's Gadgets", null, null);
        db.SellerProfiles.Add(seller);
        var idCard = UserDocument.Create(user.Id, UserDocumentType.IdCard, IdDocumentType.NationalIdCard,
            "A1234567", "user-documents/id.jpg", "id.jpg", "image/jpeg", DateTime.UtcNow);
        idCard.Approve(DateTime.UtcNow);
        db.UserDocuments.Add(idCard);
        await db.SaveChangesAsync(CancellationToken.None);
        var handler = new AdminUserCommandHandlers(
            db, new FakeCurrentUserService { UserId = Guid.NewGuid() }, new NoOpAuditLogger(), new FakeDateTime(),
            new FakePublisher());
        return (handler, db, seller);
    }

    [Fact]
    public async Task Verify_MarksTheSellerVerified()
    {
        var (handler, db, seller) = await SetupAsync();

        var result = await handler.Handle(new SetSellerVerificationCommand(seller.UserId, true, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.SellerProfiles.SingleAsync(s => s.Id == seller.Id);
        reloaded.VerificationStatus.Should().Be(VerificationStatus.Verified);
        reloaded.VerifiedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Verify_TwiceIsRefused()
    {
        var (handler, _, seller) = await SetupAsync();
        await handler.Handle(new SetSellerVerificationCommand(seller.UserId, true, null), CancellationToken.None);

        var again = await handler.Handle(new SetSellerVerificationCommand(seller.UserId, true, null), CancellationToken.None);

        again.Error.Should().Be(AdminUserErrors.AlreadyVerified);
    }

    [Fact]
    public async Task Revoke_ResetsTheStatus_ClearsVerifiedAt_AndTellsTheSellerWhy()
    {
        var (handler, db, seller) = await SetupAsync();
        await handler.Handle(new SetSellerVerificationCommand(seller.UserId, true, null), CancellationToken.None);

        var result = await handler.Handle(
            new SetSellerVerificationCommand(seller.UserId, false, "The ID photo doesn't match."), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await db.SellerProfiles.SingleAsync(s => s.Id == seller.Id);
        reloaded.VerificationStatus.Should().Be(VerificationStatus.NotStarted);
        reloaded.VerifiedAt.Should().BeNull();
        (await db.InboxNotifications.SingleAsync(n => n.UserId == seller.UserId)).Body.Should().Contain("The ID photo doesn't match.");
    }

    [Fact]
    public async Task Revoke_OfAnUnverifiedSellerIsRefused()
    {
        var (handler, _, seller) = await SetupAsync();

        var result = await handler.Handle(new SetSellerVerificationCommand(seller.UserId, false, "x"), CancellationToken.None);

        result.Error.Should().Be(AdminUserErrors.NotVerified);
    }

    [Fact]
    public async Task Verify_OfAUserWithNoSellerProfileIsRefused()
    {
        var (handler, _, _) = await SetupAsync();

        var result = await handler.Handle(new SetSellerVerificationCommand(Guid.NewGuid(), true, null), CancellationToken.None);

        result.Error.Should().Be(AdminUserErrors.NotASeller);
    }

    [Fact]
    public async Task Verify_WithoutAnApprovedIdCard_IsRefused()
    {
        var (handler, db, seller) = await SetupAsync();
        var idCard = await db.UserDocuments.SingleAsync(d => d.UserId == seller.UserId);
        db.UserDocuments.Remove(idCard);
        await db.SaveChangesAsync(CancellationToken.None);

        var result = await handler.Handle(new SetSellerVerificationCommand(seller.UserId, true, null), CancellationToken.None);

        result.Error.Should().Be(AdminUserErrors.NotReadyToVerify);
    }
}
