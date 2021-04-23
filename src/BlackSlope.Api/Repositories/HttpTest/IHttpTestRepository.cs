using System.Threading.Tasks;

namespace BlackSlope.Repositories.HttpTest
{
    public interface IHttpTestRepository
    {
        Task<dynamic> GetExponentialBackoff();
    }
}
