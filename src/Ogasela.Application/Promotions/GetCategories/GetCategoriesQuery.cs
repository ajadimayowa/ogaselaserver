using MediatR;
using Ogasela.Shared;

namespace Ogasela.Application.Promotions.GetCategories;

public sealed record GetCategoriesQuery : IRequest<Result<IReadOnlyList<CategoryResponse>>>;
