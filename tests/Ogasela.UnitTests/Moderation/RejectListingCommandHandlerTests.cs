using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Moderation.ListingReview;
using Ogasela.Domain.Listings;
using Ogasela.Domain.Payments;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Moderation;

public class RejectListingCommandHandlerTests
{
    private sealed class NoOpAuditLogger : IAuditLogger
    {
        public Task LogAsync(Guid actorId, string action, string targetType, Guid targetId,
            string? beforeJson, string? afterJson, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_PaidPendingListing_GoesBackToDraftAndRefundsThePlanCharge()
    {
        var dbContext = TestApplicationDbContext.Create();
        var dateTime = new FakeDateTime();
        var sellerId = Guid.NewGuid();

        var listing = Listing.CreateDraft(
            Guid.NewGuid(), sellerId, Guid.NewGuid(), Guid.NewGuid(), "Laptop", "Barely used laptop for sale",
            250_000m, ListingCondition.Used, ["https://example.com/a.jpg"], dateTime.UtcNow);
        listing.SubmitForReview(dateTime.UtcNow);
        dbContext.Listings.Add(listing);

        var wallet = Wallet.Create(sellerId, dateTime.UtcNow);
        dbContext.Wallets.Add(wallet);
        var charge = Transaction.CreateSucceeded(sellerId, TransactionType.PlanPurchase, 7_500_000m, null, listing.Id, dateTime.UtcNow);
        dbContext.Transactions.Add(charge);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RejectListingCommandHandler(
            dbContext, new FakeCurrentUserService { UserId = Guid.NewGuid() }, new NoOpAuditLogger(), new FakePublisher(), dateTime);

        var result = await handler.Handle(new RejectListingCommand(listing.Id, "Wrong category"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Listings.SingleAsync(l => l.Id == listing.Id);
        reloaded.Status.Should().Be(ListingStatus.Draft);
        reloaded.ReviewNote.Should().Be("Wrong category");

        (await dbContext.Wallets.SingleAsync()).BalanceKobo.Should().Be(7_500_000m);
        (await dbContext.Transactions.SingleAsync(t => t.Id == charge.Id)).Status.Should().Be(TransactionStatus.Refunded);
        (await dbContext.Transactions.CountAsync(t => t.Type == TransactionType.Refund)).Should().Be(1);
    }

    [Fact]
    public async Task Handle_ListingNotPending_IsRejected()
    {
        var dbContext = TestApplicationDbContext.Create();
        var dateTime = new FakeDateTime();
        var listing = Listing.CreateDraft(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "Chair", "A sturdy wooden chair",
            10_000m, ListingCondition.New, [], dateTime.UtcNow);
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var handler = new RejectListingCommandHandler(
            dbContext, new FakeCurrentUserService { UserId = Guid.NewGuid() }, new NoOpAuditLogger(), new FakePublisher(), dateTime);

        var result = await handler.Handle(new RejectListingCommand(listing.Id, "Spam"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Moderation.ListingNotPendingReview");
    }
}
