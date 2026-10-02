using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Promotions;
using Ogasela.Application.Promotions.CreateCategory;
using Ogasela.Application.Promotions.Interfaces;
using Ogasela.Domain.Promotions;
using Ogasela.UnitTests.TestSupport;

namespace Ogasela.UnitTests.Promotions;

public class CreateCategoryCommandTests
{
    private sealed class NoOpCategoryImageStorage : ICategoryImageStorage
    {
        public Task<string> UploadAsync(string fileName, string contentType, Stream content, CancellationToken cancellationToken) =>
            Task.FromResult($"categories/{fileName}");

        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;

        public string GetImageUrl(string key) => $"https://images.test/{key}";
    }

    private static CreateCategoryCommand Command(string name, Guid? parentId, params string[] subcategories) =>
        new(name, parentId, IsFreeEligible: true, AttributeSchemaVersion: 1, Image: null, ImageFileName: null,
            ImageContentType: null, subcategories);

    private static (CreateCategoryCommandHandler Handler, TestApplicationDbContext DbContext) CreateSut()
    {
        var dbContext = TestApplicationDbContext.Create();
        return (new CreateCategoryCommandHandler(dbContext, new NoOpCategoryImageStorage(), new FakeDateTime()), dbContext);
    }

    [Fact]
    public void Validator_TopLevelCategoryWithoutSubcategories_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator().Validate(Command("Pets", parentId: null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Add at least one subcategory.");
    }

    [Fact]
    public void Validator_DuplicateSubcategoryNames_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator().Validate(Command("Pets", null, "Dogs", " dogs "));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_SubcategoryWithItsOwnSubcategories_IsInvalid()
    {
        var result = new CreateCategoryCommandValidator().Validate(Command("Dogs", Guid.NewGuid(), "Puppies"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TopLevelCategory_SavesItWithItsSubcategories()
    {
        var (handler, dbContext) = CreateSut();

        var result = await handler.Handle(Command("Pets", null, "Dogs", "Cats"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var children = await dbContext.Categories.Where(c => c.ParentCategoryId == result.Value.Id).ToListAsync();
        children.Select(c => c.Name).Should().BeEquivalentTo("Dogs", "Cats");
        children.Should().OnlyContain(c => c.IsFreeEligible);
    }

    [Fact]
    public async Task Handle_ParentThatIsItselfASubcategory_IsRejected()
    {
        var (handler, dbContext) = CreateSut();
        var now = new FakeDateTime().UtcNow;
        var top = Category.Create(Guid.NewGuid(), "Pets", null, true, 1, null, now);
        var sub = Category.Create(Guid.NewGuid(), "Dogs", top.Id, true, 1, null, now);
        dbContext.Categories.AddRange(top, sub);
        await dbContext.SaveChangesAsync(CancellationToken.None);

        var result = await handler.Handle(Command("Puppies", sub.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(PromotionErrors.ParentMustBeTopLevel.Code);
    }
}
