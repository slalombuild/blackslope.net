using BlackSlope.Hosts.Api.Common.Validators;
using BlackSlope.Hosts.Api.Operations.Movies.Enumerators;
using BlackSlope.Hosts.Api.Operations.Movies.Requests;
using BlackSlope.Hosts.Api.Operations.Movies.Validators.Interfaces;
using FluentValidation;

namespace BlackSlope.Hosts.Api.Operations.Movies.Validators
{
    public class CreateMovieRequestValidator : BlackslopeValidator<CreateMovieRequest>, ICreateMovieRequestValidator
    {
        public CreateMovieRequestValidator()
        {
            RuleFor(x => x.Movie)
                .NotNull()
                .WithState(x => MovieErrorCode.NullRequestModel);

            RuleFor(x => x.Movie).SetValidator(new CreateMovieRequestViewModelValidator());
        }
    }
}
