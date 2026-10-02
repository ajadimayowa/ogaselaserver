using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Ogasela.Application.Accounts;
using Ogasela.Application.Accounts.ConfirmPasswordReset;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Accounts.RequestPasswordReset;
using Ogasela.Application.Notifications;
using Ogasela.Application.Notifications.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Accounts;

public class PasswordResetTests
{
    private const string Email = "Ngozi@Example.com";
    private const string Phone = "08031234567";

    private sealed class RecordingSmsSender : ISmsSender
    {
        public List<string> Sent { get; } = [];

        public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
        {
            Sent.Add(phoneNumber);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
        {
            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class PlainPasswordHasher : IPasswordHasher
    {
        public string Hash(string password) => $"hashed:{password}";

        public bool Verify(string password, string hashedPassword) => hashedPassword == Hash(password);
    }

    private sealed record Sut(
        RequestPasswordResetCommandHandler Request,
        ConfirmPasswordResetCommandHandler Confirm,
        TestApplicationDbContext DbContext,
        OtpService Otp,
        RecordingSmsSender Sms,
        RecordingEmailSender Mail);

    private static Sut CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();
        var otp = new OtpService(new FakeOtpStore());
        var sms = new RecordingSmsSender();
        var mail = new RecordingEmailSender();
        return new Sut(
            new RequestPasswordResetCommandHandler(dbContext, otp, sms, mail, NullLogger<RequestPasswordResetCommandHandler>.Instance),
            new ConfirmPasswordResetCommandHandler(dbContext, otp, new PlainPasswordHasher(), new FakeDateTime()),
            dbContext, otp, sms, mail);
    }

    private static async Task<User> SeedUserAsync(TestApplicationDbContext dbContext, UserRole role = UserRole.Seller)
    {
        var user = User.Create(Phone, Email, "hashed:old-password", role, "Ngozi");
        dbContext.Users.Add(user);
        dbContext.RefreshTokens.Add(RefreshToken.Create(user.Id, "token-hash", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)));
        await dbContext.SaveChangesAsync(CancellationToken.None);
        return user;
    }

    [Fact]
    public async Task EmailChannel_SendsCodeByEmailOnly_AndConfirmByEmailResetsPassword()
    {
        var sut = CreateSut();
        var user = await SeedUserAsync(sut.DbContext);

        var request = await sut.Request.Handle(
            new RequestPasswordResetCommand(PasswordResetChannel.Email, "ngozi@example.com", null), CancellationToken.None);

        request.IsSuccess.Should().BeTrue();
        sut.Mail.Sent.Should().ContainSingle();
        sut.Sms.Sent.Should().BeEmpty();

        var code = await sut.Otp.PeekAsync(PasswordResetOtpKey.ForEmail(Email), CancellationToken.None);
        var confirm = await sut.Confirm.Handle(
            new ConfirmPasswordResetCommand(null, code!, "new-password", Email: "NGOZI@example.com"), CancellationToken.None);

        confirm.IsSuccess.Should().BeTrue();
        (await sut.DbContext.Users.SingleAsync(u => u.Id == user.Id)).PasswordHash.Should().Be("hashed:new-password");
        (await sut.DbContext.RefreshTokens.AllAsync(t => t.RevokedAt != null)).Should().BeTrue();
    }

    [Fact]
    public async Task PhoneChannel_SendsCodeBySmsOnly_AndConfirmByPhoneResetsPassword()
    {
        var sut = CreateSut();
        var user = await SeedUserAsync(sut.DbContext);

        await sut.Request.Handle(new RequestPasswordResetCommand(PasswordResetChannel.Phone, null, Phone), CancellationToken.None);

        sut.Sms.Sent.Should().ContainSingle().Which.Should().Be(Phone);
        sut.Mail.Sent.Should().BeEmpty();

        var code = await sut.Otp.PeekAsync(Phone, CancellationToken.None);
        var confirm = await sut.Confirm.Handle(new ConfirmPasswordResetCommand(Phone, code!, "new-password"), CancellationToken.None);

        confirm.IsSuccess.Should().BeTrue();
        (await sut.DbContext.Users.SingleAsync(u => u.Id == user.Id)).PasswordHash.Should().Be("hashed:new-password");
    }

    [Fact]
    public async Task Request_ForUnknownEmail_SucceedsWithoutSendingAnything()
    {
        var sut = CreateSut();

        var result = await sut.Request.Handle(
            new RequestPasswordResetCommand(PasswordResetChannel.Email, "nobody@example.com", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sut.Mail.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Request_ForStaffAccount_SendsNothing()
    {
        var sut = CreateSut();
        await SeedUserAsync(sut.DbContext, UserRole.Moderator);

        var result = await sut.Request.Handle(
            new RequestPasswordResetCommand(PasswordResetChannel.Email, Email, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        sut.Mail.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Confirm_WithEmailCodeAgainstPhone_IsRejected()
    {
        var sut = CreateSut();
        await SeedUserAsync(sut.DbContext);
        await sut.Request.Handle(new RequestPasswordResetCommand(PasswordResetChannel.Email, Email, null), CancellationToken.None);
        var emailCode = await sut.Otp.PeekAsync(PasswordResetOtpKey.ForEmail(Email), CancellationToken.None);

        var confirm = await sut.Confirm.Handle(new ConfirmPasswordResetCommand(Phone, emailCode!, "new-password"), CancellationToken.None);

        confirm.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ConfirmValidator_RequiresExactlyOneOfPhoneOrEmail()
    {
        var validator = new ConfirmPasswordResetCommandValidator();

        validator.Validate(new ConfirmPasswordResetCommand(null, "123456", "new-password")).IsValid.Should().BeFalse();
        validator.Validate(new ConfirmPasswordResetCommand(Phone, "123456", "new-password", Email)).IsValid.Should().BeFalse();
        validator.Validate(new ConfirmPasswordResetCommand(null, "123456", "new-password", Email)).IsValid.Should().BeTrue();
    }
}
