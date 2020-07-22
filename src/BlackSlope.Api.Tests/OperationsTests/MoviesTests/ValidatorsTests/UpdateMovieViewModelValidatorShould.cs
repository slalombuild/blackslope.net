using AutoFixture;
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
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Title, _modelViewModel);
        }

        [Fact]
        public void Fail_when_description_is_null()
        {
            _modelViewModel.Description = null;
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Description, _modelViewModel);
        }

        [Fact]
        public void Fail_when_title_is_not_between_2_and_50_characters()
        {
            _modelViewModel.Title = "2";
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Title.Length, _modelViewModel);
        }

        [Fact]
        public void Fail_when_description_is_not_between_2_and_50_characters()
        {
            _modelViewModel.Description = "d";
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Description.Length, _modelViewModel);
        }

        [Fact]
        public void Pass_when_title_is_between_2_and_50_characters()
        {
            _modelViewModel.Title = "the post";
            _movieViewModelValidator.ShouldNotHaveValidationErrorFor(x => x.Title.Length, _modelViewModel);
        }

        [Fact]
        public void Pass_when_description_is_between_2_and_50_characters()
        {
            _modelViewModel.Description = "Great movie";
            _movieViewModelValidator.ShouldNotHaveValidationErrorFor(x => x.Description, _modelViewModel);
        }

    }
}
