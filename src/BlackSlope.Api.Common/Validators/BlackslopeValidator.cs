using System;
using FluentValidation;
using System.Linq;
using FluentValidation.Results;
using BlackSlope.Api.Common.ViewModels;
using BlackSlope.Api.Common.Enumerators;
using FluentValidation.Internal;
using System.Threading.Tasks;
using System.Threading;
using BlackSlope.Api.Common.Extensions;

namespace BlackSlope.Api.Common.Validators
{
    public abstract class BlackslopeValidator<T> : AbstractValidator<T>
    {
        public new void Validate(T instance)
        {
            var validationResult = base.Validate(instance);
            HandleValidationFailure(validationResult, instance);
        }

        public new async Task ValidateAsync(T instance, CancellationToken cancellation = default)
        {
            var validationResult = await base.ValidateAsync(instance);
            HandleValidationFailure(validationResult, instance);
        }

        public void Validate(T instance, params string[] ruleSetsToExecute)
        {
            var ruleSetValidatorSelector = new RulesetValidatorSelector(ruleSetsToExecute);
            var validationContext = new ValidationContext<T>(instance, null, ruleSetValidatorSelector);

            var validationResult = base.Validate(validationContext);
            HandleValidationFailure(validationResult, instance);
        }

        private void HandleValidationFailure(ValidationResult result, object instance)
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
            if (validationFailure.CustomState is Enum)
            {
                errorCode = (int)validationFailure.CustomState;
                message = ((Enum) validationFailure.CustomState).GetDescription();
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
