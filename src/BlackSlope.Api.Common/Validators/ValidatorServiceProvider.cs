using System;
using System.Linq;
using BlackSlope.Api.Common.Validators.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BlackSlope.Api.Common.Validators
{
    public class ValidatorServiceProvider : IValidatorAbstractFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public ValidatorServiceProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IValidator<T> Resolve<T>()
        {
            var validators = _serviceProvider.GetServices(typeof(IValidator<T>))
                .OfType<IValidator<T>>()
                .ToList();

            switch (validators.Count)
            {
                case 0:
                    return null;
                case 1:
                    return validators.First();
                default:
                    return new CompositeValidator<T>(validators);
            }
        }
    }
}
