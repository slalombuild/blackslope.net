using System.Reflection;
using BlackSlope.Api.Common.Validators;
using BlackSlope.Api.Common.Validators.Interfaces;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BlackSlope.Api.Extensions
{
    public static class BlackSlopeValidatorExtensions
    {
        /// <summary>
        /// Adds black slope validator to service collection
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.TryAddTransient<IBlackSlopeValidator, BlackSlopeValidator>();
            services.TryAddTransient<IValidatorAbstractFactory, ValidatorServiceLocator>();

            services.AddValidatorsFromAssembly(Assembly.GetEntryAssembly());
            return services;
        }
    }
}
