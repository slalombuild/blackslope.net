using System;
using System.Collections.Generic;
using FluentValidation;

namespace BlackSlope.Api.Common.Validators
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix", Justification = "Composite makes the intention clearer")]
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
