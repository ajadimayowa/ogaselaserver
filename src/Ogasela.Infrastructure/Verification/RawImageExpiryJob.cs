using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Verification.Interfaces;

namespace Ogasela.Infrastructure.Verification;

/// <summary>
/// Daily Hangfire recurring job that enforces the 30-day raw-image retention policy: any
/// verification record past its <see cref="Domain.Verification.BiometricVerification.RawImageExpiryDate"/>
/// has its S3 objects deleted and its key references nulled out, keeping only the
/// Rekognition face id and the decision/scores behind - never the raw images themselves.
/// </summary>
public sealed class RawImageExpiryJob
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IBiometricImageStorage _imageStorage;
    private readonly IDateTime _dateTime;
    private readonly ILogger<RawImageExpiryJob> _logger;

    public RawImageExpiryJob(
        IApplicationDbContext dbContext, IBiometricImageStorage imageStorage, IDateTime dateTime,
        ILogger<RawImageExpiryJob> logger)
    {
        _dbContext = dbContext;
        _imageStorage = imageStorage;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = _dateTime.UtcNow;

        var expired = await _dbContext.BiometricVerifications
            .Where(v => !v.IsErased
                        && v.RawImageExpiryDate != null
                        && v.RawImageExpiryDate <= now
                        && (v.SelfieImageS3Key != null || v.IdPhotoImageS3Key != null))
            .ToListAsync();

        foreach (var verification in expired)
        {
            if (verification.SelfieImageS3Key is { } selfieKey)
            {
                await _imageStorage.DeleteAsync(selfieKey, CancellationToken.None);
            }

            if (verification.IdPhotoImageS3Key is { } idPhotoKey)
            {
                await _imageStorage.DeleteAsync(idPhotoKey, CancellationToken.None);
            }

            verification.ExpireRawImages();
        }

        if (expired.Count > 0)
        {
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation(
                "Purged raw biometric images for {Count} expired verification record(s)", expired.Count);
        }
    }
}
