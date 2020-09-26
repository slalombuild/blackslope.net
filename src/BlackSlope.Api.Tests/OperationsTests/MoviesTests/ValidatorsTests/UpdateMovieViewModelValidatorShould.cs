using AutoFixture;
using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.Validators;
using BlackSlope.Api.Operations.Movies.ViewModels;
using FluentValidation.TestHelper;
using Xunit;

namespace BlackSlope.Api.Tests.OperationsTests.MoviesTests.ValidatorsTests
{
    public class UpdateMovieViewModelValidatorShould
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly MovieViewModel _modelViewModel;
        private readonly UpdateMovieViewModelValidatorCollection _movieViewModelValidator;

        public UpdateMovieViewModelValidatorShould()
        {
            _modelViewModel = _fixture.Create<MovieViewModel>();
            _movieViewModelValidator = new UpdateMovieViewModelValidatorCollection();
        }


        [Fact]
        public void Fail_when_title_is_null()
        {
            _modelViewModel.Title = null;
            _movieViewModelValidator
                .ShouldHaveValidationErrorFor(x => x.Title, _modelViewModel)
                .When(failure => MovieErrorCode.EmptyOrNullMovieTitle.Equals(failure.CustomState)); ;
        }

        [Fact]
        public void Fail_when_description_is_null()
        {
            _modelViewModel.Description = null;
            _movieViewModelValidator
                .ShouldHaveValidationErrorFor(x => x.Description, _modelViewModel)
                .When(failure => MovieErrorCode.EmptyOrNullMovieDescription.Equals(failure.CustomState));
        }

        [Theory]
        [InlineData("d")]
        [InlineData("A great movie title that is very thrilling but sadly too long.")]
        public void Fail_when_title_is_not_between_2_and_50_characters(string title)
        {
            _modelViewModel.Title = title;
            _movieViewModelValidator
                .ShouldHaveValidationErrorFor(x => x.Title.Length, _modelViewModel)
                .When(failure => MovieErrorCode.TitleNotBetween2and50Characters.Equals(failure.CustomState));
        }

        [Theory]
        [InlineData("d")]
        [InlineData("A great movie description that is very descriptive but sadly too long.")]
        public void Fail_when_description_is_not_between_2_and_50_characters(string description)
        {
            _modelViewModel.Description = description;
            _movieViewModelValidator
                .ShouldHaveValidationErrorFor(x => x.Description.Length, _modelViewModel)
                .When(failure => MovieErrorCode.DescriptionNotBetween2and50Characters.Equals(failure.CustomState));
        }

        [Fact]
        public void Pass_when_title_is_between_2_and_50_characters()
        {
            _modelViewModel.Title = "the post";
            _movieViewModelValidator.ShouldNotHaveValidationErrorFor(x => x.Title, _modelViewModel);
        }

        [Fact]
        public void Pass_when_description_is_between_2_and_50_characters()
        {
            _modelViewModel.Description = "Great movie";
            _movieViewModelValidator.ShouldNotHaveValidationErrorFor(x => x.Description, _modelViewModel);
        }
    }
}
