using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Accounts.SocialLogin;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Shared;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Accounts;

public class SocialLoginCommandHandlerTests
{
    private sealed class FakeVerifier : ISocialTokenVerifier
    {
        public SocialIdentity? Identity { get; set; }

        public Task<Result<SocialIdentity>> VerifyAsync(ExternalLoginProvider provider, string token, CancellationToken cancellationToken) =>
            Task.FromResult(Identity is null
                ? Result.Failure<SocialIdentity>(AccountErrors.SocialTokenInvalid)
                : Result.Success(Identity));
    }

    private sealed class PlainPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hashedPassword) => hashedPassword == Hash(password);
    }

    private sealed class NoOpEmailSender : IEmailSender
    {
        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private static (SocialLoginCommandHandler Handler, TestApplicationDbContext DbContext, FakeVerifier Verifier) CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();
        var dateTime = new FakeDateTime();
        var verifier = new FakeVerifier();
        var handler = new SocialLoginCommandHandler(
            dbContext, verifier, new PlainPasswordHasher(),
            new AuthTokenIssuer(dbContext, new FakeTokenService(), dateTime), dateTime,
            new NoOpEmailSender(), NullLogger<SocialLoginCommandHandler>.Instance);
        return (handler, dbContext, verifier);
    }

    private static SocialLoginCommand Command(string? name = null, UserRole accountType = UserRole.Seller) =>
        new(ExternalLoginProvider.Google, "provider-token", name, accountType);

    [Fact]
    public async Task Handle_NewIdentity_CreatesSellerWithProfileAndLink()
    {
        var (handler, dbContext, verifier) = CreateSut();
        verifier.Identity = new SocialIdentity("google-sub-1", "Ada@Example.com", true, "Ada Obi");

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNewUser.Should().BeTrue();
        result.Value.AccessToken.Should().NotBeNullOrEmpty();

        var user = await dbContext.Users.SingleAsync();
        user.Email.Should().Be("ada@example.com");
        user.Name.Should().Be("Ada Obi");
        user.Role.Should().Be(UserRole.Seller);
        (await dbContext.SellerProfiles.SingleAsync()).BusinessName.Should().Be("Ada Obi");
        (await dbContext.ExternalLogins.SingleAsync()).ProviderUserId.Should().Be("google-sub-1");
    }

    [Fact]
    public async Task Handle_SameIdentityAgain_SignsIntoTheSameAccount()
    {
        var (handler, dbContext, verifier) = CreateSut();
        verifier.Identity = new SocialIdentity("google-sub-2", "bola@example.com", true, "Bola");
        await handler.Handle(Command(), CancellationToken.None);

        var second = await handler.Handle(Command(), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        second.Value.IsNewUser.Should().BeFalse();
        (await dbContext.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Handle_VerifiedEmailOfExistingAccount_LinksInsteadOfCreating()
    {
        var (handler, dbContext, verifier) = CreateSut();
        var existing = User.Create("08031234567", "chi@example.com", "hashed:pw", UserRole.Buyer, "Chi");
        dbContext.Users.Add(existing);
        await dbContext.SaveChangesAsync(CancellationToken.None);
        verifier.Identity = new SocialIdentity("google-sub-3", "CHI@example.com", true, "Chi");

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsNewUser.Should().BeFalse();
        (await dbContext.Users.CountAsync()).Should().Be(1);
        (await dbContext.ExternalLogins.SingleAsync()).UserId.Should().Be(existing.Id);
    }

    [Fact]
    public async Task Handle_UnverifiedEmailOfExistingAccount_IsRejected()
    {
        var (handler, dbContext, verifier) = CreateSut();
        dbContext.Users.Add(User.Create(null, "dayo@example.com", "hashed:pw", UserRole.Buyer));
        await dbContext.SaveChangesAsync(CancellationToken.None);
        verifier.Identity = new SocialIdentity("google-sub-4", "dayo@example.com", false, null);

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.SocialEmailBelongsToAnotherAccount.Code);
        (await dbContext.ExternalLogins.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task Handle_EmailOfAnInternalAccount_IsRejected()
    {
        var (handler, dbContext, verifier) = CreateSut();
        dbContext.Users.Add(User.Create("08030000000", "admin@ogasela.com", "hashed:pw", UserRole.SuperAdmin));
        await dbContext.SaveChangesAsync(CancellationToken.None);
        verifier.Identity = new SocialIdentity("google-sub-5", "admin@ogasela.com", true, "Admin");

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.SocialLoginNotAllowed.Code);
    }

    [Fact]
    public async Task Handle_InvalidToken_IsRejected()
    {
        var (handler, _, verifier) = CreateSut();
        verifier.Identity = null;

        var result = await handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.SocialTokenInvalid.Code);
    }

    [Fact]
    public async Task Handle_AppleWithoutNameInToken_UsesTheNameTheAppSent()
    {
        var (handler, dbContext, verifier) = CreateSut();
        verifier.Identity = new SocialIdentity("apple-sub-1", null, false, null);

        var result = await handler.Handle(Command(name: "Emeka Nwosu", accountType: UserRole.Buyer), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var user = await dbContext.Users.SingleAsync();
        user.Name.Should().Be("Emeka Nwosu");
        user.Email.Should().BeNull();
        user.Role.Should().Be(UserRole.Buyer);
        (await dbContext.SellerProfiles.AnyAsync()).Should().BeFalse();
    }
}
