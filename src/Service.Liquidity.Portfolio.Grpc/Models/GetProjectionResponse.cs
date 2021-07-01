using System.Runtime.Serialization;
using Service.Liquidity.Portfolio.Domain.Models;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetProjectionResponse
    {
        [DataMember(Order = 1)] public bool Success { get; set; }
        [DataMember(Order = 2)] public GetProjectionRequest Request { get; set; }
        [DataMember(Order = 3)] public double ProjectionVolume { get; set; }
        [DataMember(Order = 4)] public string ErrorText { get; set; }
    }
}
