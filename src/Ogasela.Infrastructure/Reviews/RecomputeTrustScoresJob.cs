using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ogasela.Application.Common.Interfaces;
using Ogasela.Domain.Accounts;
using Ogasela.Domain.Reviews;

namespace Ogasela.Infrastructure.Reviews;

/// <summary>
/// Nightly Hangfire job. Recomputes every seller's 0-100 TrustScore from three inputs.
///
/// THIS IS A FIRST-PASS HEURISTIC, NOT FINAL - the weights and the response-rate proxy below
/// are reasonable starting points to tune once real usage data exists, not a considered model.
///
///   Score = VerificationComponent + RatingComponent + ResponseComponent   (each capped, sums to at most 100)
///
///   - VerificationComponent (0 or 20): a flat boost for a Verified seller - being verified is
///     a binary trust signal, not something to average.
///   - RatingComponent (0-50): (average review rating / 5) * 50. A seller with no reviews yet
///     gets the midpoint (25) rather than 0, so being new isn't scored identically to being bad.
///   - ResponseComponent (0-30): (fraction of conversations replied to within 24h) * 30, using
///     "seller sent at least one message within 24h of the conversation starting" as the simple
///     proxy the phase asked for. A seller with no conversations gets the midpoint (15) for the
///     same reason as above.
///
/// Note: per-seller this runs a handful of queries (reviews, conversations, one exists-check per
/// conversation) rather than one aggregate query - readable over optimal for a first pass; revisit
/// if the nightly run time becomes a problem as the seller base grows.
/// </summary>
public sealed class RecomputeTrustScoresJob
{
    private const decimal VerificationBoost = 20m;
    private const decimal MaxRatingComponent = 50m;
    private const decimal MaxResponseComponent = 30m;
    private static readonly TimeSpan ResponseWindow = TimeSpan.FromHours(24);

    private readonly IApplicationDbContext _dbContext;
    private readonly IDateTime _dateTime;
    private readonly ILogger<RecomputeTrustScoresJob> _logger;

    public RecomputeTrustScoresJob(IApplicationDbContext dbContext, IDateTime dateTime, ILogger<RecomputeTrustScoresJob> logger)
    {
        _dbContext = dbContext;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        var now = _dateTime.UtcNow;
        var sellers = await _dbContext.SellerProfiles.ToListAsync();

        foreach (var seller in sellers)
        {
            var score = await ComputeScoreAsync(seller, now);

            var trustScore = await _dbContext.TrustScores.FirstOrDefaultAsync(t => t.SellerId == seller.Id);
            if (trustScore is null)
            {
                _dbContext.TrustScores.Add(TrustScore.Create(seller.Id, score, now));
            }
            else
            {
                trustScore.Update(score, now);
            }
        }

        await _dbContext.SaveChangesAsync(CancellationToken.None);
        _logger.LogInformation("Recomputed trust scores for {Count} seller(s)", sellers.Count);
    }

    private async Task<decimal> ComputeScoreAsync(SellerProfile seller, DateTime now)
    {
        var verificationComponent = seller.VerificationStatus == VerificationStatus.Verified ? VerificationBoost : 0m;

        var ratings = await _dbContext.Reviews
            .Where(r => r.RevieweeId == seller.UserId)
            .Select(r => r.Rating)
            .ToListAsync();

        var ratingComponent = ratings.Count == 0
            ? MaxRatingComponent / 2m
            : (decimal)ratings.Average() / 5m * MaxRatingComponent;

        var conversations = await _dbContext.Conversations
            .Where(c => c.SellerId == seller.UserId)
            .Select(c => new { c.Id, c.CreatedAt })
            .ToListAsync();

        decimal responseComponent;
        if (conversations.Count == 0)
        {
            responseComponent = MaxResponseComponent / 2m;
        }
        else
        {
            var respondedCount = 0;
            foreach (var conversation in conversations)
            {
                var deadline = conversation.CreatedAt.Add(ResponseWindow);
                var responded = await _dbContext.Messages.AnyAsync(m =>
                    m.ConversationId == conversation.Id && m.SenderId == seller.UserId && m.SentAt <= deadline);

                if (responded)
                {
                    respondedCount++;
                }
            }

            responseComponent = (decimal)respondedCount / conversations.Count * MaxResponseComponent;
        }

        return verificationComponent + ratingComponent + responseComponent;
    }
}
