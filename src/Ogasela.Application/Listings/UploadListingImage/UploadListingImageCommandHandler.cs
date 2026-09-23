using MediatR;
using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.UploadListingImage;

/// <summary>
/// Returns a permanent public URL the client then includes in
/// CreateListingRequest/UpdateListingRequest.MediaUrls. Not tied to a specific listing (there's
/// no listing to attach to yet when a seller is still composing a draft) - the only reason this
/// touches the database at all is to look up the current seller's business name for the watermark.
/// </summary>
public sealed class UploadListingImageCommandHandler : IRequestHandler<UploadListingImageCommand, Result<UploadListingImageResponse>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUser;
    private readonly IListingImageProcessor _imageProcessor;
    private readonly IListingImageStorage _imageStorage;

    public UploadListingImageCommandHandler(
        IApplicationDbContext dbContext,
        ICurrentUserService currentUser,
        IListingImageProcessor imageProcessor,
        IListingImageStorage imageStorage)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _imageProcessor = imageProcessor;
        _imageStorage = imageStorage;
    }

    public async Task<Result<UploadListingImageResponse>> Handle(UploadListingImageCommand request, CancellationToken cancellationToken)
    {
        var businessName = await _dbContext.SellerProfiles
            .Where(s => s.UserId == _currentUser.UserId!.Value)
            .Select(s => s.BusinessName)
            .FirstOrDefaultAsync(cancellationToken);

        if (businessName is null)
        {
            return Result.Failure<UploadListingImageResponse>(ListingErrors.SellerProfileNotFound);
        }

        var processed = await _imageProcessor.ProcessAsync(request.Content, $"Ogasela • {businessName}", cancellationToken);
        await using var _ = processed.Content;

        var fileName = $"{Path.GetFileNameWithoutExtension(request.FileName)}{processed.FileExtension}";
        var url = await _imageStorage.UploadAsync(fileName, processed.ContentType, processed.Content, cancellationToken);
        return Result.Success(new UploadListingImageResponse(url));
    }
}
