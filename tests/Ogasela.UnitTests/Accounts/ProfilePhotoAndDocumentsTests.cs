using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Accounts.Interfaces;
using Ogasela.Application.Accounts.ProfilePhoto;
using Ogasela.Application.Accounts.UserDocuments;
using Ogasela.Application.Verification.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Accounts;

public class ProfilePhotoAndDocumentsTests
{
    private sealed class FakePhotoStorage : IProfilePhotoStorage
    {
        public List<string> Deleted { get; } = [];
        private int _counter;

        public Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
            Task.FromResult($"profile-photos/{++_counter}");

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            Deleted.Add(key);
            return Task.CompletedTask;
        }

        public string GetImageUrl(string key) => $"https://files.test/{key}";
    }

    /// <summary>Only CheckQualityAsync is used by the profile photo flow.</summary>
    private sealed class FakeFaceProvider(FaceQualityResult result) : IFaceVerificationProvider
    {
        public Task<FaceQualityResult> CheckQualityAsync(string imageRef, CancellationToken cancellationToken) => Task.FromResult(result);
        public Task<string> CreateLivenessSessionAsync(CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<LivenessResult> CheckLivenessAsync(string livenessSessionRef, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<FaceMatchResult> CompareFacesAsync(string selfieRef, string idPhotoRef, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<Guid?> FindDuplicateAsync(string selfieRef, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> EnrollFaceAsync(string selfieRef, Guid sellerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task DeleteFaceAsync(string faceId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static async Task<(TestApplicationDbContext Db, User User)> SeedAsync()
    {
        var db = TestApplicationDbContext.Create();
        var user = User.Create("08031234567", "ada@example.com", "hash", UserRole.Seller, "Ada");
        db.Users.Add(user);
        await db.SaveChangesAsync(CancellationToken.None);
        return (db, user);
    }

    [Fact]
    public async Task UploadPhoto_WithAClearFace_SetsItAndRemovesThePreviousPhoto()
    {
        var (db, user) = await SeedAsync();
        user.SetProfilePhoto("profile-photos/old");
        await db.SaveChangesAsync(CancellationToken.None);
        var storage = new FakePhotoStorage();
        var handler = new UploadProfilePhotoCommandHandler(
            db, new FakeCurrentUserService { UserId = user.Id }, storage, new FakeFaceProvider(new FaceQualityResult(true, null)));

        var result = await handler.Handle(new UploadProfilePhotoCommand(Stream.Null, "me.jpg", "image/jpeg"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await db.Users.SingleAsync()).ProfilePhotoS3Key.Should().Be("profile-photos/1");
        storage.Deleted.Should().Equal("profile-photos/old");
    }

    [Fact]
    public async Task UploadPhoto_WithoutAClearFace_IsRejectedAndTheUploadDeleted()
    {
        var (db, user) = await SeedAsync();
        var storage = new FakePhotoStorage();
        var handler = new UploadProfilePhotoCommandHandler(
            db, new FakeCurrentUserService { UserId = user.Id }, storage, new FakeFaceProvider(new FaceQualityResult(false, "No face detected")));

        var result = await handler.Handle(new UploadProfilePhotoCommand(Stream.Null, "wall.jpg", "image/jpeg"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.ProfilePhotoRejected");
        result.Error.Message.Should().Contain("no face detected");
        (await db.Users.SingleAsync()).ProfilePhotoS3Key.Should().BeNull();
        storage.Deleted.Should().Equal("profile-photos/1");
    }

    private static UploadUserDocumentCommand Doc(UserDocumentType type, IdDocumentType? idType, string? idNumber, string contentType = "application/pdf") =>
        new(type, idType, idNumber, Stream.Null, "doc.pdf", contentType);

    [Theory]
    [InlineData(UserDocumentType.IdCard, IdDocumentType.NationalIdCard, "A1234567", "image/jpeg", true)]
    [InlineData(UserDocumentType.IdCard, IdDocumentType.InternationalPassport, "B98765432", "application/pdf", true)]
    [InlineData(UserDocumentType.IdCard, null, "A1234567", "image/jpeg", false)]
    [InlineData(UserDocumentType.IdCard, IdDocumentType.VotersCard, null, "image/jpeg", false)]
    [InlineData(UserDocumentType.IdCard, IdDocumentType.VotersCard, "12-34", "image/jpeg", false)]
    [InlineData(UserDocumentType.UtilityBill, null, null, "application/pdf", true)]
    [InlineData(UserDocumentType.UtilityBill, IdDocumentType.NinSlip, "12345678", "application/pdf", false)]
    [InlineData(UserDocumentType.UtilityBill, null, null, "text/plain", false)]
    public void DocumentRules(UserDocumentType type, IdDocumentType? idType, string? idNumber, string contentType, bool valid) =>
        new UploadUserDocumentCommandValidator().Validate(Doc(type, idType, idNumber, contentType)).IsValid.Should().Be(valid);
}
