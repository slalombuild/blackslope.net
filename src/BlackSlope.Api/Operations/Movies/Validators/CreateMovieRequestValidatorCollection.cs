using BlackSlope.Api.Common.Validators;
using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.Requests;
using BlackSlope.Api.Operations.Movies.Validators.Interfaces;
using BlackSlope.Services.Movies;
using FluentValidation;

namespace BlackSlope.Api.Operations.Movies.Validators
{
    public class CreateMovieRequestValidatorCollection : BlackslopeValidatorCollection<CreateMovieRequest>, ICreateMovieRequestValidator
    {
        public CreateMovieRequestValidatorCollection(IMovieService movieService)
        {
            RuleFor(x => x.Movie)
                .NotNull()
                .WithState(x => MovieErrorCode.NullRequestViewModel)
                .DependentRules(() => ValidateViewModel(movieService));
        }

        private void ValidateViewModel(IMovieService movieService)
        {
            RuleFor(x => x.Movie).SetValidator(new CreateMovieViewModelValidatorCollection(movieService));
        }
    }
}
