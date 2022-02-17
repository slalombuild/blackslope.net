using AutoFixture;
using BlackSlope.Api.Operations.Movies.Enumerators;
using BlackSlope.Api.Operations.Movies.Validators;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies;
using FluentValidation.TestHelper;
using Moq;
using Xunit;

namespace BlackSlope.Api.Tests.OperationsTests.MoviesTests.ValidatorsTests
{
    public class CreateMovieViewModelValidatorShould
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly CreateMovieViewModel _modelViewModel;
        private readonly CreateMovieViewModelValidatorCollection _movieViewModelValidator;
        private readonly Mock<IMovieService> _movieService = new Mock<IMovieService>();

        public CreateMovieViewModelValidatorShould()
        {
            _modelViewModel = _fixture.Create<CreateMovieViewModel>();
            _movieViewModelValidator = new CreateMovieViewModelValidatorCollection(_movieService.Object);
        }

        [Fact]
        public void Fail_when_title_is_null()
        {
            _modelViewModel.Title = null;
            var result = _movieViewModelValidator.TestValidate(_modelViewModel);
            var failures = result
                .ShouldHaveValidationErrorFor(x => x.Title)
                .When(failure => MovieErrorCode.EmptyOrNullMovieTitle.Equals(failure.CustomState));
        }

        [Fact]
        public void Fail_when_description_is_null()
        {
            _modelViewModel.Description = null;
            var result = _movieViewModelValidator.TestValidate(_modelViewModel);
            result
                .ShouldHaveValidationErrorFor(x => x.Description)
                .When(failure => MovieErrorCode.EmptyOrNullMovieDescription.Equals(failure.CustomState));
        }

        [Theory]
        [InlineData("d")]
        [InlineData("A great movie title that is very thrilling but sadly too long.")]
        public void Fail_when_title_is_not_between_2_and_50_characters(string title)
        {
            _modelViewModel.Title = title;
            var result = _movieViewModelValidator.TestValidate(_modelViewModel);
            result
                .ShouldHaveValidationErrorFor(x => x.Title.Length)
                .When(failure => MovieErrorCode.TitleNotBetween2and50Characters.Equals(failure.CustomState));
        }

        [Theory]
        [InlineData("d")]
        [InlineData("A great movie description that is very descriptive but sadly too long.")]
        public void Fail_when_description_is_not_between_2_and_50_characters(string description)
        {
            _modelViewModel.Description = description;
            var result = _movieViewModelValidator.TestValidate(_modelViewModel);
            result.ShouldHaveValidationErrorFor(x => x.Description.Length)
                .When(failure => MovieErrorCode.DescriptionNotBetween2and50Characters.Equals(failure.CustomState));
        }

        [Fact]
        public void Pass_when_title_is_between_2_and_50_characters()
        {
            _modelViewModel.Title = "the post";
            var result = _movieViewModelValidator.TestValidate(_modelViewModel);
            result.ShouldNotHaveValidationErrorFor(x => x.Title);
        }

        [Fact]
        public void Pass_when_description_is_between_2_and_50_characters()
        {
            _modelViewModel.Description = "Great movie";
            var result = _movieViewModelValidator.TestValidate(_modelViewModel);
            result.ShouldNotHaveValidationErrorFor(x => x.Description);
        }
    }
}
