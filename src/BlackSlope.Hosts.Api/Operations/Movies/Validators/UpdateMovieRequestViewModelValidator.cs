using BlackSlope.Hosts.Api.Common.Validators;
using BlackSlope.Hosts.Api.Operations.Movies.Enumerators;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using FluentValidation;

namespace BlackSlope.Hosts.Api.Operations.Movies.Validators
{
    public class UpdateMovieRequestViewModelValidator : BlackslopeValidator<UpdateMovieRequestViewModel>
    {
        public UpdateMovieRequestViewModelValidator()
        {
            RuleFor(x => x.Id)
               .NotEmpty()
               .WithState(x => MovieErrorCode.EmptyOrNullMovieId);

            RuleFor(x => x.Title)
                .NotEmpty()
                .WithState(x => MovieErrorCode.EmptyOrNullMovieTitle);

            RuleFor(x => x.Title)
                .MinimumLength(2).MaximumLength(50)
                .WithState(x => MovieErrorCode.TitleNotBetween2and50Characters);

            RuleFor(x => x.Description)
                .NotEmpty()
                .WithState(x => MovieErrorCode.EmptyOrNullMovieDescription);

            RuleFor(x => x.Description)
                .MinimumLength(2).MaximumLength(50)
                .WithState(x => MovieErrorCode.DescriptionNotBetween2and50Characters);
        }
    }
}
