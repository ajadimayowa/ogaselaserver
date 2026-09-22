using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.GetPromotionPlans;

public sealed record GetPromotionPlansQuery : IRequest<Result<IReadOnlyList<PromotionPlanResponse>>>;
