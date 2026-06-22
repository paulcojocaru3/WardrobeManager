using FluentValidation;
using WardrobeManager.Application.Clothing.Queries;

namespace WardrobeManager.Application.Clothing.Validators;

public sealed class GetWearStatisticsQueryValidator : AbstractValidator<GetWearStatisticsQuery>
{
    public GetWearStatisticsQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Range).MaximumLength(20);
    }
}
