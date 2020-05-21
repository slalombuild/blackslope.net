using FluentValidation;

namespace BlackSlope.Api.Common.Validators.Interfaces
{
    public interface IValidatorAbstractFactory
    {
        IValidator<T> Resolve<T>();
    }
}
