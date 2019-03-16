using FluentValidation;
using System.Linq;
using FluentValidation.Results;
using BlackSlope.Hosts.Api.Common.ViewModels;
using BlackSlope.Hosts.Api.Common.Enumerators;
using FluentValidation.Internal;

namespace BlackSlope.Hosts.Api.Common.Validators
{
    public abstract class BlackslopeValidator<T> : AbstractValidator<T>
    {
        public new void Validate(T instance)
        {
            var validationResult = base.Validate(instance);
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
                var errors = result.Errors.Select(e => new ApiError
                {
                    Code = (int)ApiHttpStatusCode.BadRequest,
                    Message = e.ErrorMessage
                }).ToList();

                throw new ApiException(ApiHttpStatusCode.BadRequest, instance, errors);
            }
        }
    }
}
