using System;
using AutoFixture;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using BlackSlope.Hosts.Api.Operations.Movies.Validators;
using Xunit;
using FluentValidation.TestHelper;

namespace Blackslope.Hosts.Api.Tests
{
    public class CreateMovieRequestViewModelValidatorTests : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();
        private CreateMovieRequestViewModel _modelViewModel;
        private CreateMovieRequestViewModelValidator _movieViewModelValidator;
        public CreateMovieRequestViewModelValidatorTests()
        {
            _modelViewModel = _fixture.Create<CreateMovieRequestViewModel>();
            _movieViewModelValidator = new CreateMovieRequestViewModelValidator();
        }

        public void Dispose()
        {
        }

        [Fact]
        public void Validation_should_fail_when_title_is_null()
        {
            _modelViewModel.Title = null;
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Title, _modelViewModel);
        }

        [Fact]
        public void Validation_should_fail_when_description_is_null()
        {
            _modelViewModel.Description = null;
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Description, _modelViewModel);
        }

        [Fact]
        public void Validation_should_fail_when_title_is_not_between_2_and_50_characters()
        {
            _modelViewModel.Title = "2";
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Title, _modelViewModel);
        }

        [Fact]
        public void Validation_should_fail_when_description_is_not_between_2_and_50_characters()
        {
            _modelViewModel.Description = "d";
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Description, _modelViewModel);
        }

        [Fact]
        public void Validation_should_pass_when_title_is_between_2_and_50_characters()
        {
            _modelViewModel.Title = "the post";
            _movieViewModelValidator.ShouldNotHaveValidationErrorFor(x => x.Title, _modelViewModel);
        }

        [Fact]
        public void Validation_should_pass_when_description_is_between_2_and_50_characters()
        {
            _modelViewModel.Description = "Great movie";
            _movieViewModelValidator.ShouldNotHaveValidationErrorFor(x => x.Description, _modelViewModel);
        }
    }
}
