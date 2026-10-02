using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.ConfirmAdminPasswordReset;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Accounts;

public class ConfirmAdminPasswordResetCommandHandlerTests
{
    private const string Email = "admin@ogasela.com";
    private const string Phone = "08031234567";

    private sealed class PlainPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
    }

    private static (ConfirmAdminPasswordResetCommandHandler Handler, TestApplicationDbContext DbContext, OtpService OtpService)
        CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();
        var otpService = new OtpService(new FakeOtpStore());
        var handler = new ConfirmAdminPasswordResetCommandHandler(dbContext, otpService, new PlainPasswordHasher(), new FakeDateTime());

        return (handler, dbContext, otpService);
    }

    private static async Task<User> SeedUserAsync(TestApplicationDbContext dbContext, UserRole role)
    {
        var user = User.Create(Phone, Email, "hashed:old-password", role);
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(RefreshToken.Create(user.Id, "token-hash", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task Handle_WithValidCode_SetsPasswordAndRevokesSessions()
    {
        var (handler, dbContext, otpService) = CreateSut();
        var user = await SeedUserAsync(dbContext, UserRole.Moderator);
        await otpService.GenerateAsync(Phone, CancellationToken.None);
        var code = await otpService.PeekAsync(Phone, CancellationToken.None);

        var result = await handler.Handle(new ConfirmAdminPasswordResetCommand(Email, code!, "new-password"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var reloaded = await dbContext.Users.SingleAsync(u => u.Id == user.Id);
        reloaded.PasswordHash.Should().Be("hashed:new-password");
        (await dbContext.RefreshTokens.AllAsync(t => t.RevokedAt != null)).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithWrongCode_LeavesPasswordUnchanged()
    {
        var (handler, dbContext, otpService) = CreateSut();
        var user = await SeedUserAsync(dbContext, UserRole.SuperAdmin);
        await otpService.GenerateAsync(Phone, CancellationToken.None);

        var result = await handler.Handle(new ConfirmAdminPasswordResetCommand(Email, "000000x", "new-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.OtpInvalid.Code);
        (await dbContext.Users.SingleAsync(u => u.Id == user.Id)).PasswordHash.Should().Be("hashed:old-password");
    }

    [Fact]
    public async Task Handle_ForNonInternalAccount_FailsLikeAWrongCode()
    {
        var (handler, dbContext, otpService) = CreateSut();
        await SeedUserAsync(dbContext, UserRole.Buyer);
        await otpService.GenerateAsync(Phone, CancellationToken.None);
        var code = await otpService.PeekAsync(Phone, CancellationToken.None);

        var result = await handler.Handle(new ConfirmAdminPasswordResetCommand(Email, code!, "new-password"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(AccountErrors.OtpInvalid.Code);
    }
}
