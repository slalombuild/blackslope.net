using System;
using AutoFixture;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using BlackSlope.Hosts.Api.Operations.Movies.Validators;
using Xunit;
using FluentValidation.TestHelper;

namespace Blackslope.Hosts.Api.Tests
{
    public class UpdateMovieRequestViewModelValidatorTests : IDisposable
    {
        private readonly Fixture _fixture = new Fixture();
        private UpdateMovieRequestViewModel _modelViewModel;
        private UpdateMovieRequestViewModelValidator _movieViewModelValidator;
        public UpdateMovieRequestViewModelValidatorTests()
        {
            _modelViewModel = _fixture.Create<UpdateMovieRequestViewModel>();
            _movieViewModelValidator = new UpdateMovieRequestViewModelValidator();
        }

        public void Dispose()
        {
        }

        [Fact]
        public void Validation_should_fail_when_id_is_empty()
        {
            _modelViewModel.Id = 0;
            _movieViewModelValidator.ShouldHaveValidationErrorFor(x => x.Id, _modelViewModel);
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
