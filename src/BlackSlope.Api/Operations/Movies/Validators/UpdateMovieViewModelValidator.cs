using BlackSlope.Api.Common.Validators;
using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.ViewModels;
using FluentValidation;

namespace BlackSlope.Api.Operations.Movies.Validators
{
    public class UpdateMovieViewModelValidator : BlackslopeValidator<MovieViewModel>
    {
        public UpdateMovieViewModelValidator()
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithState(x => MovieErrorCode.EmptyOrNullMovieTitle)
                .DependentRules(() =>
                    RuleFor(x => x.Title.Length)
                        .InclusiveBetween(2, 50).WithState(x => MovieErrorCode.TitleNotBetween2and50Characters));

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithState(x => MovieErrorCode.EmptyOrNullMovieDescription)
                .DependentRules(() =>
                    RuleFor(x => x.Description.Length)
                        .InclusiveBetween(2, 50).WithState(x => MovieErrorCode.TitleNotBetween2and50Characters));
        }
    }
}
