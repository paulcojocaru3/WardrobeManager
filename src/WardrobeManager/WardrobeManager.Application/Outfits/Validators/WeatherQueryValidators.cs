using FluentValidation;
using WardrobeManager.Application.Outfits.Queries;

namespace WardrobeManager.Application.Outfits.Validators;

public sealed class GetCurrentWeatherQueryValidator : AbstractValidator<GetCurrentWeatherQuery>
{
    public GetCurrentWeatherQueryValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
    }
}

public sealed class GetWeatherForecastQueryValidator : AbstractValidator<GetWeatherForecastQuery>
{
    public GetWeatherForecastQueryValidator()
    {
        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Days).InclusiveBetween(1, 30);
    }
}

public sealed class SearchCitiesQueryValidator : AbstractValidator<SearchCitiesQuery>
{
    public SearchCitiesQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(120);
    }
}
