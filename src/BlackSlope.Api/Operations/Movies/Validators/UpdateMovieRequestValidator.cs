using BlackSlope.Api.Common.Validators;
using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.Requests;
using BlackSlope.Api.Operations.Movies.Validators.Interfaces;
using BlackSlope.Api.Operations.Movies.ViewModels;
using FluentValidation;

namespace BlackSlope.Api.Operations.Movies.Validators
{
    public class UpdateMovieRequestValidator : BlackslopeValidator<UpdateMovieRequest>, IUpdateMovieRequestValidator
    {
        public UpdateMovieRequestValidator()
        {
            RuleFor(r => r.Movie)
                .NotNull()
                .WithState(_ => MovieErrorCode.NullRequestViewModel)
                .DependentRules(() => ValidateViewModel());
        }

        private void ValidateViewModel()
        {
            RuleFor(x => x.Id).Must((x, id) => !HasIdConfilict(id, x.Movie))
                .WithState(_ => MovieErrorCode.IdConflict);
            RuleFor(x => x.Id).Must((x, id) => HasAnId(id, x.Movie))
              .WithState(_ => MovieErrorCode.EmptyOrNullMovieId);
            RuleFor(x => x.Movie).SetValidator(new UpdateMovieViewModelValidator());
        }

        private bool HasAnId(int? id, MovieViewModel request)
            => id != null || request.Id != null;

        private bool HasIdConfilict(int? id, MovieViewModel request)
            => id != null && request.Id != null && id != request.Id;
    }
}
