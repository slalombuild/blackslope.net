using System.Threading.Tasks;

namespace BlackSlope.Repositories.FakeApi
{
    public interface IFakeApiRepository
    {
        Task<dynamic> GetExponentialBackoff();
    }
}
