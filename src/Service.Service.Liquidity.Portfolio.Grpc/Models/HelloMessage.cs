using System.Runtime.Serialization;
using Service.Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class HelloMessage : IHelloMessage
    {
        [DataMember(Order = 1)]
        public string Message { get; set; }
    }
}