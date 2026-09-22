using Microsoft.EntityFrameworkCore;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Application.Listings;
using Ogasela.Domain.AdIntegrations;
using Ogasela.Domain.Listings;
using Ogasela.Shared;

namespace Ogasela.Application.AdIntegrations.Internal;

/// <summary>Small lookups shared by every AdIntegrations command/query handler, kept in one place so the ownership/active-row rules stay consistent across all of them.</summary>
internal static class AdIntegrationLookups
{
    public static async Task<Result<Listing>> GetOwnedListingAsync(
        IApplicationDbContext dbContext, ICurrentUserService currentUser, Guid listingId, CancellationToken cancellationToken)
    {
        var listing = await dbContext.Listings.FirstOrDefaultAsync(l => l.Id == listingId, cancellationToken);
        if (listing is null)
        {
            return Result.Failure<Listing>(ListingErrors.ListingNotFound);
        }

        var isOwner = await dbContext.SellerProfiles
            .AnyAsync(s => s.Id == listing.SellerId && s.UserId == currentUser.UserId!.Value, cancellationToken);
        if (!isOwner)
        {
            return Result.Failure<Listing>(ListingErrors.NotOwner);
        }

        return Result.Success(listing);
    }

    /// <summary>The currently-active connection for a seller+platform, if any (a seller can reconnect after revoking, leaving older rows behind for history).</summary>
    public static Task<AdAccountConnection?> GetActiveConnectionAsync(
        IApplicationDbContext dbContext, Guid sellerId, AdPlatform platform, CancellationToken cancellationToken) =>
        dbContext.AdAccountConnections
            .Where(c => c.SellerId == sellerId && c.Platform == platform && c.RevokedAt == null)
            .OrderByDescending(c => c.ConnectedAt)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Like <see cref="GetActiveConnectionAsync"/>, but distinguishes "never connected"
    /// (ConnectionNotFound) from "connected, then revoked" (ConnectionRevoked) rather than
    /// treating both as a bare null - used by every command that acts on an existing connection,
    /// so a revoked seller gets a clear "reconnect" error instead of a generic not-found.
    /// </summary>
    public static async Task<Result<AdAccountConnection>> RequireActiveConnectionAsync(
        IApplicationDbContext dbContext, Guid sellerId, AdPlatform platform, CancellationToken cancellationToken)
    {
        var connection = await dbContext.AdAccountConnections
            .Where(c => c.SellerId == sellerId && c.Platform == platform)
            .OrderByDescending(c => c.ConnectedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (connection is null)
        {
            return Result.Failure<AdAccountConnection>(AdIntegrationErrors.ConnectionNotFound);
        }

        if (!connection.IsActive)
        {
            return Result.Failure<AdAccountConnection>(AdIntegrationErrors.ConnectionRevoked);
        }

        return Result.Success(connection);
    }

    /// <summary>The most recently created campaign for a listing+platform (a listing may accumulate more than one over time, e.g. after being reposted and re-promoted).</summary>
    public static Task<AdCampaign?> GetLatestCampaignAsync(
        IApplicationDbContext dbContext, Guid listingId, AdPlatform platform, CancellationToken cancellationToken) =>
        dbContext.AdCampaigns
            .Where(c => c.ListingId == listingId && c.Platform == platform)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
}
