using FluentValidation;
using Ogasela.Application.Analytics.GetListingAnalytics;
using Ogasela.Application.Analytics.GetSellerAnalytics;

namespace Ogasela.Application.Analytics;

public sealed class GetSellerAnalyticsQueryValidator : AbstractValidator<GetSellerAnalyticsQuery>
{
    public GetSellerAnalyticsQueryValidator()
    {
        RuleFor(x => x.Days).Must(d => AnalyticsPeriod.AllowedDays.Contains(d)).WithMessage("Days must be 7, 30 or 90.");
    }
}

public sealed class GetListingAnalyticsQueryValidator : AbstractValidator<GetListingAnalyticsQuery>
{
    public GetListingAnalyticsQueryValidator()
    {
        RuleFor(x => x.Days).Must(d => AnalyticsPeriod.AllowedDays.Contains(d)).WithMessage("Days must be 7, 30 or 90.");
    }
}
