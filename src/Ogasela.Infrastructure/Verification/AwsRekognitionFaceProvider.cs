using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ogasela.Application.Verification.Interfaces;

namespace Ogasela.Infrastructure.Verification;

/// <summary>
/// Production <see cref="IFaceVerificationProvider"/> backed by AWS Rekognition. All image refs
/// are S3 keys within the configured bucket - Rekognition reads images directly from S3 rather
/// than having raw bytes pass through this service a second time.
/// </summary>
public sealed class AwsRekognitionFaceProvider : IFaceVerificationProvider
{
    private const decimal MinimumQualityBrightness = 40m;
    private const decimal MinimumQualitySharpness = 40m;
    private const decimal MinimumFaceConfidence = 90m;

    private readonly IAmazonRekognition _rekognitionClient;
    private readonly AwsSettings _awsSettings;
    private readonly RekognitionSettings _rekognitionSettings;
    private readonly ILogger<AwsRekognitionFaceProvider> _logger;

    public AwsRekognitionFaceProvider(
        IAmazonRekognition rekognitionClient,
        IOptions<AwsSettings> awsSettings,
        IOptions<RekognitionSettings> rekognitionSettings,
        ILogger<AwsRekognitionFaceProvider> logger)
    {
        _rekognitionClient = rekognitionClient;
        _awsSettings = awsSettings.Value;
        _rekognitionSettings = rekognitionSettings.Value;
        _logger = logger;
    }

    public async Task<string> CreateLivenessSessionAsync(CancellationToken cancellationToken)
    {
        var response = await _rekognitionClient.CreateFaceLivenessSessionAsync(
            new CreateFaceLivenessSessionRequest(), cancellationToken);

        return response.SessionId;
    }

    public async Task<FaceQualityResult> CheckQualityAsync(string imageRef, CancellationToken cancellationToken)
    {
        var response = await _rekognitionClient.DetectFacesAsync(
            new DetectFacesRequest
            {
                Image = ToImage(imageRef),
                Attributes = [Amazon.Rekognition.Attribute.ALL]
            },
            cancellationToken);

        if (response.FaceDetails.Count != 1)
        {
            return new FaceQualityResult(
                false, response.FaceDetails.Count == 0 ? "No face detected" : "More than one face detected");
        }

        var face = response.FaceDetails[0];

        if (face.Confidence < (double)MinimumFaceConfidence)
        {
            return new FaceQualityResult(false, "Face detection confidence too low");
        }

        if (face.Quality.Brightness < (double)MinimumQualityBrightness)
        {
            return new FaceQualityResult(false, "Image too dark");
        }

        if (face.Quality.Sharpness < (double)MinimumQualitySharpness)
        {
            return new FaceQualityResult(false, "Image too blurry");
        }

        return new FaceQualityResult(true, null);
    }

    public async Task<LivenessResult> CheckLivenessAsync(string livenessSessionRef, CancellationToken cancellationToken)
    {
        var response = await _rekognitionClient.GetFaceLivenessSessionResultsAsync(
            new GetFaceLivenessSessionResultsRequest { SessionId = livenessSessionRef }, cancellationToken);

        var score = (decimal)response.Confidence;
        var passed = response.Status == LivenessSessionStatus.SUCCEEDED && score >= 90m;

        return new LivenessResult(passed, score);
    }

    public async Task<FaceMatchResult> CompareFacesAsync(string selfieRef, string idPhotoRef, CancellationToken cancellationToken)
    {
        var response = await _rekognitionClient.CompareFacesAsync(
            new CompareFacesRequest
            {
                SourceImage = ToImage(idPhotoRef),
                TargetImage = ToImage(selfieRef),
                SimilarityThreshold = 0
            },
            cancellationToken);

        if (response.FaceMatches.Count == 0)
        {
            return new FaceMatchResult(false, 0m);
        }

        var bestMatch = response.FaceMatches.OrderByDescending(m => m.Similarity).First();
        var similarity = (decimal)bestMatch.Similarity;

        return new FaceMatchResult(similarity >= _rekognitionSettings.FaceMatchThreshold, similarity);
    }

    public async Task<Guid?> FindDuplicateAsync(string selfieRef, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _rekognitionClient.SearchFacesByImageAsync(
                new SearchFacesByImageRequest
                {
                    CollectionId = _rekognitionSettings.CollectionId,
                    Image = ToImage(selfieRef),
                    FaceMatchThreshold = (float)_rekognitionSettings.DuplicateMatchThreshold,
                    MaxFaces = 1
                },
                cancellationToken);

            var match = response.FaceMatches.FirstOrDefault();
            if (match is null || !Guid.TryParse(match.Face.ExternalImageId, out var sellerId))
            {
                return null;
            }

            return sellerId;
        }
        catch (InvalidParameterException)
        {
            // Rekognition throws this when no face meets the search threshold in an otherwise
            // valid image - treat it the same as "no duplicate found".
            return null;
        }
        catch (ResourceNotFoundException)
        {
            // The collection doesn't exist yet (first-ever enrollment on a fresh environment).
            _logger.LogWarning(
                "Rekognition collection {CollectionId} not found while searching for duplicates",
                _rekognitionSettings.CollectionId);
            return null;
        }
    }

    public async Task<string> EnrollFaceAsync(string selfieRef, Guid sellerId, CancellationToken cancellationToken)
    {
        var response = await _rekognitionClient.IndexFacesAsync(
            new IndexFacesRequest
            {
                CollectionId = _rekognitionSettings.CollectionId,
                Image = ToImage(selfieRef),
                ExternalImageId = sellerId.ToString(),
                MaxFaces = 1,
                QualityFilter = QualityFilter.AUTO,
                DetectionAttributes = []
            },
            cancellationToken);

        var faceRecord = response.FaceRecords.FirstOrDefault()
            ?? throw new InvalidOperationException("Rekognition did not index a face for the enrollment image.");

        return faceRecord.Face.FaceId;
    }

    public async Task DeleteFaceAsync(string rekognitionFaceId, CancellationToken cancellationToken)
    {
        await _rekognitionClient.DeleteFacesAsync(
            new DeleteFacesRequest
            {
                CollectionId = _rekognitionSettings.CollectionId,
                FaceIds = [rekognitionFaceId]
            },
            cancellationToken);
    }

    private Image ToImage(string s3Key) => new()
    {
        S3Object = new S3Object { Bucket = _awsSettings.S3BucketName, Name = s3Key }
    };
}
