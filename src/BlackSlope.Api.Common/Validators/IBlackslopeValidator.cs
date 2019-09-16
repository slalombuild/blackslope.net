using System.Threading;
using System.Threading.Tasks;

namespace BlackSlope.Api.Common.Validators
{
    public interface IBlackslopeValidator<in T>
    {
        void Validate(T instance);

        void Validate(T instance, string[] ruleSetsToExecute);

        Task ValidateAsync(T instance, CancellationToken cancellation = default);
    }
}
