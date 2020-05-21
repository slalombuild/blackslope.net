using System;
using System.Linq;
using System.Threading.Tasks;
using BlackSlope.Api.Common.Enumerators;
using BlackSlope.Api.Common.Extensions;
using BlackSlope.Api.Common.Validators.Interfaces;
using BlackSlope.Api.Common.ViewModels;
using FluentValidation;
using FluentValidation.Internal;
using FluentValidation.Results;

namespace BlackSlope.Api.Common.Validators
{
    public class BlackSlopeValidator : IBlackSlopeValidator
    {
        private readonly IValidatorAbstractFactory _validatorAbstractFactory;

        public BlackSlopeValidator(IValidatorAbstractFactory validatorAbstractFactory)
        {
            _validatorAbstractFactory = validatorAbstractFactory;
        }

        public void AssertValid<T>(T instance, params string[] ruleSetsToExecute)
        {
            var ruleSetValidatorSelector = new RulesetValidatorSelector(ruleSetsToExecute);
            var validationContext = new ValidationContext<T>(instance, null, ruleSetValidatorSelector);

            var validator = _validatorAbstractFactory.Resolve<T>();
            var validationResult = validator.Validate(validationContext);

            HandleValidationFailure(validationResult, instance);
        }

        public void AssertValid<T>(T instance)
        {
            var validator = _validatorAbstractFactory.Resolve<T>();
            var validationResult = validator.Validate(instance);

            HandleValidationFailure(validationResult, instance);
        }

        public async Task AssertValidAsync<T>(T instance)
        {
            var validator = _validatorAbstractFactory.Resolve<T>();
            var validationResult = await validator.ValidateAsync(instance);

            HandleValidationFailure(validationResult, instance);
        }

        private static void HandleValidationFailure(ValidationResult result, object instance)
        {
            if (!result.IsValid)
            {
                var errors = result.Errors.Select(CreateApiError).ToList();
                throw new ApiException(ApiHttpStatusCode.BadRequest, instance, errors);
            }
        }

        private static ApiError CreateApiError(ValidationFailure validationFailure)
        {
            int errorCode;
            string message = null;
            if (validationFailure.CustomState is Enum validationFailureEnum)
            {
                errorCode = (int)validationFailure.CustomState;
                message = validationFailureEnum.GetDescription();
            }
            else
            {
                errorCode = (int)ApiHttpStatusCode.BadRequest;
            }

            return new ApiError
            {
                Code = errorCode,
                Message = string.IsNullOrEmpty(message)
                    ? validationFailure.ErrorMessage
                    : message,
            };
        }
    }
}
