using BlackSlope.Api.Common.Validators;
using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.Requests;
using BlackSlope.Api.Operations.Movies.Validators.Interfaces;
using BlackSlope.Api.Operations.Movies.ViewModels;
using FluentValidation;

namespace BlackSlope.Api.Operations.Movies.Validators
{
    public class UpdateMovieRequestValidatorCollection : BlackslopeValidatorCollection<UpdateMovieRequest>, IUpdateMovieRequestValidator
    {
        public UpdateMovieRequestValidatorCollection()
        {
            RuleFor(r => r.Movie)
                .NotNull()
                .WithState(_ => MovieErrorCode.NullRequestViewModel)
                .DependentRules(() => ValidateViewModel());
        }

        private static bool HasAnId(int? id, MovieViewModel request)
          => id != null || request.Id != null;

        private static bool HasIdConfilict(int? id, MovieViewModel request)
            => id != null && request.Id != null && id != request.Id;

        private void ValidateViewModel()
        {
            RuleFor(x => x.Id).Must((x, id) => !HasIdConfilict(id, x.Movie))
                .WithState(_ => MovieErrorCode.IdConflict);
            RuleFor(x => x.Id).Must((x, id) => HasAnId(id, x.Movie))
              .WithState(_ => MovieErrorCode.EmptyOrNullMovieId);
            RuleFor(x => x.Movie).SetValidator(new UpdateMovieViewModelValidatorCollection());
        }
    }
}
