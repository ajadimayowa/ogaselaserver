using FluentAssertions;
using Microsoft.Extensions.Options;
using Ogasela.Application.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Accounts;

public class OtpServiceTests
{
    private const string Phone = "08031234567";

    [Fact]
    public async Task VerifyAsync_WithCorrectCode_ReturnsSuccess()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);
        var code = await sut.PeekAsync(Phone, CancellationToken.None);

        var result = await sut.VerifyAsync(Phone, code!, CancellationToken.None);

        result.Should().Be(OtpVerificationResult.Success);
    }

    [Fact]
    public async Task VerifyAsync_WithWrongCode_ReturnsInvalidCode()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);

        var result = await sut.VerifyAsync(Phone, "000000", CancellationToken.None);

        result.Should().Be(OtpVerificationResult.InvalidCode);
    }

    [Fact]
    public async Task VerifyAsync_WithNoCodeRequested_ReturnsExpired()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);

        var result = await sut.VerifyAsync(Phone, "123456", CancellationToken.None);

        result.Should().Be(OtpVerificationResult.Expired);
    }

    [Fact]
    public async Task VerifyAsync_SucceedingAttempt_ConsumesTheCode()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);
        var code = await sut.PeekAsync(Phone, CancellationToken.None);

        await sut.VerifyAsync(Phone, code!, CancellationToken.None);
        var secondAttempt = await sut.VerifyAsync(Phone, code!, CancellationToken.None);

        secondAttempt.Should().Be(OtpVerificationResult.Expired);
    }

    [Fact]
    public async Task VerifyAsync_AllowsUpToFiveAttemptsWithinTheWindow()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);

        for (var i = 0; i < 5; i++)
        {
            var result = await sut.VerifyAsync(Phone, "000000", CancellationToken.None);
            result.Should().Be(OtpVerificationResult.InvalidCode, because: $"attempt {i + 1} is within the allowed limit");
        }
    }

    [Fact]
    public async Task VerifyAsync_SixthAttemptWithinWindow_IsRateLimited()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);

        for (var i = 0; i < 5; i++)
        {
            await sut.VerifyAsync(Phone, "000000", CancellationToken.None);
        }

        var sixthAttempt = await sut.VerifyAsync(Phone, "000000", CancellationToken.None);

        sixthAttempt.Should().Be(OtpVerificationResult.RateLimited);
    }

    [Fact]
    public async Task VerifyAsync_RateLimited_EvenWithTheCorrectCode()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);
        var code = await sut.PeekAsync(Phone, CancellationToken.None);

        for (var i = 0; i < 5; i++)
        {
            await sut.VerifyAsync(Phone, "000000", CancellationToken.None);
        }

        var sixthAttempt = await sut.VerifyAsync(Phone, code!, CancellationToken.None);

        sixthAttempt.Should().Be(OtpVerificationResult.RateLimited);
    }

    [Fact]
    public async Task VerifyAsync_SuccessfulVerification_ResetsAttemptCounter()
    {
        var store = new FakeOtpStore();
        var sut = new OtpService(store);
        await sut.GenerateAsync(Phone, CancellationToken.None);
        var firstCode = await sut.PeekAsync(Phone, CancellationToken.None);

        await sut.VerifyAsync(Phone, "000000", CancellationToken.None);
        await sut.VerifyAsync(Phone, "000000", CancellationToken.None);
        await sut.VerifyAsync(Phone, firstCode!, CancellationToken.None);

        await sut.GenerateAsync(Phone, CancellationToken.None);
        var secondCode = await sut.PeekAsync(Phone, CancellationToken.None);

        var result = await sut.VerifyAsync(Phone, secondCode!, CancellationToken.None);

        result.Should().Be(OtpVerificationResult.Success);
    }

    private const string MasterCode = "246810";

    private static OtpService WithMasterCode(FakeOtpStore store, string? masterCode = MasterCode) =>
        new(store, Options.Create(new OtpSettings { MasterCode = masterCode }));

    [Fact]
    public async Task VerifyAsync_WithMasterCode_ReturnsSuccessEvenWhenNoCodeWasSent()
    {
        var sut = WithMasterCode(new FakeOtpStore());

        var result = await sut.VerifyAsync(Phone, MasterCode, CancellationToken.None);

        result.Should().Be(OtpVerificationResult.Success);
    }

    [Fact]
    public async Task VerifyAsync_WithMasterCodeConfigured_StillAcceptsTheGeneratedCode()
    {
        var sut = WithMasterCode(new FakeOtpStore());
        await sut.GenerateAsync(Phone, CancellationToken.None);
        var code = await sut.PeekAsync(Phone, CancellationToken.None);

        var result = await sut.VerifyAsync(Phone, code!, CancellationToken.None);

        result.Should().Be(OtpVerificationResult.Success);
    }

    [Fact]
    public async Task VerifyAsync_WithMasterCodeConfigured_StillRejectsOtherWrongCodes()
    {
        var sut = WithMasterCode(new FakeOtpStore());
        await sut.GenerateAsync(Phone, CancellationToken.None);

        var result = await sut.VerifyAsync(Phone, "000000", CancellationToken.None);

        result.Should().Be(OtpVerificationResult.InvalidCode);
    }

    [Fact]
    public async Task VerifyAsync_WithEmptyMasterCode_DoesNotAcceptAnything()
    {
        var sut = WithMasterCode(new FakeOtpStore(), masterCode: "");

        var result = await sut.VerifyAsync(Phone, "", CancellationToken.None);

        result.Should().Be(OtpVerificationResult.Expired);
    }

    [Fact]
    public async Task VerifyAsync_WithMasterCode_IsStillRateLimited()
    {
        var sut = WithMasterCode(new FakeOtpStore());
        for (var i = 0; i < 5; i++)
        {
            await sut.VerifyAsync(Phone, "000000", CancellationToken.None);
        }

        var result = await sut.VerifyAsync(Phone, MasterCode, CancellationToken.None);

        result.Should().Be(OtpVerificationResult.RateLimited);
    }
}
