using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Listings.UploadListingImage;

public sealed record UploadListingImageCommand(
    Stream Content, string FileName, string ContentType) : IRequest<Result<UploadListingImageResponse>>;

public sealed record UploadListingImageResponse(string Url);
