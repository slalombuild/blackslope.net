using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies;
using FluentValidation;

namespace BlackSlope.Api.Operations.Movies.Validators
{
    public class CreateMovieViewModelValidatorCollection : AbstractValidator<CreateMovieViewModel>
    {
        public CreateMovieViewModelValidatorCollection(IMovieService movieService)
        {
            RuleFor(x => x).MustAsync(async (x, cancellationtoken) =>
                    !await movieService.CheckIfMovieExistsAsync(x.Title, x.ReleaseDate))
                .WithState(x => MovieErrorCode.MovieAlreadyExists);

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
                        .InclusiveBetween(2, 50).WithState(x => MovieErrorCode.DescriptionNotBetween2and50Characters));
        }
    }
}
