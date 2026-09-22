using MediatR;
using Ogasela.Application.Listings.Interfaces;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.UploadListingImage;

/// <summary>
/// A bare upload utility - returns a permanent public URL the client then includes in
/// CreateListingRequest/UpdateListingRequest.MediaUrls. Not tied to a specific listing (there's
/// no listing to attach to yet when a seller is still composing a draft), so this doesn't touch
/// the database at all.
/// </summary>
public sealed class UploadListingImageCommandHandler : IRequestHandler<UploadListingImageCommand, Result<UploadListingImageResponse>>
{
    private readonly IListingImageStorage _imageStorage;

    public UploadListingImageCommandHandler(IListingImageStorage imageStorage)
    {
        _imageStorage = imageStorage;
    }

    public async Task<Result<UploadListingImageResponse>> Handle(UploadListingImageCommand request, CancellationToken cancellationToken)
    {
        var url = await _imageStorage.UploadAsync(request.FileName, request.ContentType, request.Content, cancellationToken);
        return Result.Success(new UploadListingImageResponse(url));
    }
}
