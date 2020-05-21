using System;
using System.Collections.Generic;
using FluentValidation;

namespace BlackSlope.Api.Common.Validators
{
    public class CompositeValidator<T> : AbstractValidator<T>
    {
        public CompositeValidator(IEnumerable<IValidator<T>> validators)
        {
            if (validators is null)
            {
                throw new ArgumentNullException(nameof(validators));
            }

            foreach (var validator in validators)
            {
                Include(validator);
            }
        }
    }
}
