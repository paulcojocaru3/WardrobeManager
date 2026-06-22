using MediatR;

namespace WardrobeManager.Application.Clothing.Queries;

public sealed record GetArticleSubtypesQuery : IRequest<Dictionary<string, List<string>>>;
