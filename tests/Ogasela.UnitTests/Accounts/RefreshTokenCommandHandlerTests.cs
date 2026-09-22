using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.Refresh;
using Ogasela.Domain.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Accounts;

public class RefreshTokenCommandHandlerTests
{
    private static (RefreshTokenCommandHandler Handler, TestApplicationDbContext DbContext, FakeTokenService TokenService, FakeDateTime DateTime)
        CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();
        var tokenService = new FakeTokenService();
        var dateTime = new FakeDateTime();
        var issuer = new AuthTokenIssuer(dbContext, tokenService, dateTime);
        var handler = new RefreshTokenCommandHandler(dbContext, tokenService, dateTime, issuer);

        return (handler, dbContext, tokenService, dateTime);
    }

    private static async Task<(User User, RefreshToken Token, string RawToken)> SeedActiveTokenAsync(
        TestApplicationDbContext dbContext, FakeTokenService tokenService, FakeDateTime dateTime)
    {
        var user = User.Create("08031234567", null, "hash", UserRole.Buyer);
        dbContext.Users.Add(user);

        var rawToken = tokenService.GenerateRefreshTokenValue();
        var token = RefreshToken.Create(user.Id, tokenService.HashRefreshToken(rawToken), dateTime.UtcNow.AddDays(30));
        dbContext.RefreshTokens.Add(token);

        await dbContext.SaveChangesAsync(CancellationToken.None);

        return (user, token, rawToken);
    }

    [Fact]
    public async Task Handle_WithValidActiveToken_RotatesItAndIssuesANewPair()
    {
        var (handler, dbContext, tokenService, dateTime) = CreateSut();
        var (_, oldToken, rawToken) = await SeedActiveTokenAsync(dbContext, tokenService, dateTime);

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.RefreshToken.Should().NotBe(rawToken);

        var reloadedOldToken = await dbContext.RefreshTokens.SingleAsync(t => t.Id == oldToken.Id);
        reloadedOldToken.IsRevoked.Should().BeTrue();
        reloadedOldToken.WasRotated.Should().BeTrue();

        var newTokenHash = tokenService.HashRefreshToken(result.Value.RefreshToken);
        var newToken = await dbContext.RefreshTokens.SingleAsync(t => t.TokenHash == newTokenHash);
        newToken.IsRevoked.Should().BeFalse();
        reloadedOldToken.ReplacedByTokenId.Should().Be(newToken.Id);
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsInvalid()
    {
        var (handler, _, _, _) = CreateSut();

        var result = await handler.Handle(new RefreshTokenCommand("does-not-exist"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.RefreshTokenInvalid.Code);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsExpired()
    {
        var (handler, dbContext, tokenService, dateTime) = CreateSut();
        var (_, _, rawToken) = await SeedActiveTokenAsync(dbContext, tokenService, dateTime);

        dateTime.UtcNow = dateTime.UtcNow.AddDays(31);

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.RefreshTokenExpired.Code);
    }

    [Fact]
    public async Task Handle_WithLoggedOutToken_ReturnsInvalid_NotReused()
    {
        var (handler, dbContext, tokenService, dateTime) = CreateSut();
        var (_, token, rawToken) = await SeedActiveTokenAsync(dbContext, tokenService, dateTime);

        // Simulate logout: revoked directly, never rotated into a replacement.
        token.Revoke(dateTime.UtcNow);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.RefreshTokenInvalid.Code);
    }

    [Fact]
    public async Task Handle_WhenARotatedTokenIsPresentedAgain_DetectsReuseAndRevokesTheWholeFamily()
    {
        var (handler, dbContext, tokenService, dateTime) = CreateSut();
        var (user, firstToken, firstRawToken) = await SeedActiveTokenAsync(dbContext, tokenService, dateTime);

        // Legitimate rotation: refresh once, then a second, independent active session.
        var firstRefresh = await handler.Handle(new RefreshTokenCommand(firstRawToken), CancellationToken.None);
        firstRefresh.IsSuccess.Should().BeTrue();

        var secondSessionRawToken = tokenService.GenerateRefreshTokenValue();
        var secondSessionToken = RefreshToken.Create(
            user.Id, tokenService.HashRefreshToken(secondSessionRawToken), dateTime.UtcNow.AddDays(30));
        dbContext.RefreshTokens.Add(secondSessionToken);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        // An attacker (or a client with a stale token) replays the already-rotated first token.
        var reuseResult = await handler.Handle(new RefreshTokenCommand(firstRawToken), CancellationToken.None);

        reuseResult.IsFailure.Should().BeTrue();
        reuseResult.Error.Code.Should().Be(AccountErrors.RefreshTokenReused.Code);

        var latestRotatedToken = await dbContext.RefreshTokens.SingleAsync(t => t.Id == firstToken.ReplacedByTokenId);
        latestRotatedToken.IsRevoked.Should().BeTrue("the entire token family must be revoked on reuse detection");

        var reloadedSecondSessionToken = await dbContext.RefreshTokens.SingleAsync(t => t.Id == secondSessionToken.Id);
        reloadedSecondSessionToken.IsRevoked.Should().BeTrue("reuse detection must revoke every other active token for the user");
    }
}
