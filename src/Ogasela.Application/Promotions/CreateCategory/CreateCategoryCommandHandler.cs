using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Promotions.GetCategories;
using Ogasela.Application.Promotions.Interfaces;
using Ogasela.Domain.Promotions;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.CreateCategory;

public sealed class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, Result<CategoryResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICategoryImageStorage _imageStorage;
    private readonly IDateTime _dateTime;

    public CreateCategoryCommandHandler(
        IApplicationDbContext dbContext, ICategoryImageStorage imageStorage, IDateTime dateTime)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
        _dateTime = dateTime;
    }

    public async Task<Result<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        if (request.ParentCategoryId is { } parentId)
        {
            var parentExists = await _dbContext.Categories.AnyAsync(c => c.Id == parentId, cancellationToken);
            if (!parentExists)
            {
                return Result.Failure<CategoryResponse>(PromotionErrors.ParentCategoryNotFound);
            }
        }

        string? imageKey = null;
        if (request.Image is not null)
        {
            imageKey = await _imageStorage.UploadAsync(
                request.ImageFileName!, request.ImageContentType!, request.Image, cancellationToken);
        }

        var now = _dateTime.UtcNow;
        var category = Category.Create(
            Guid.NewGuid(), request.Name, request.ParentCategoryId, request.IsFreeEligible,
            request.AttributeSchemaVersion, imageKey, now);

        _dbContext.Categories.Add(category);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new CategoryResponse(
            category.Id,
            category.Name,
            category.ParentCategoryId,
            category.IsFreeEligible,
            category.AttributeSchemaVersion,
            imageKey is null ? null : _imageStorage.GetImageUrl(imageKey)));
    }
}
