using System.ServiceModel;
using System.Threading.Tasks;
using Service.Service.Liquidity.Portfolio.Grpc.Models;

namespace Service.Service.Liquidity.Portfolio.Grpc
{
    [ServiceContract]
    public interface IHelloService
    {
        [OperationContract]
        Task<HelloMessage> SayHelloAsync(HelloRequest request);
    }
}