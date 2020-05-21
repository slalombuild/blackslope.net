using System.Threading.Tasks;

namespace BlackSlope.Api.Common.Validators.Interfaces
{
    public interface IBlackSlopeValidator
    {
        void AssertValid<T>(T instance);

        Task AssertValidAsync<T>(T instance);

        void AssertValid<T>(T instance, string[] ruleSetsToExecute);
    }
}
