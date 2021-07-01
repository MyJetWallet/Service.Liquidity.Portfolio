using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class GetProjectionRequest
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string FromAsset { get; set; }
        [DataMember(Order = 3)] public string ToAsset { get; set; }
        [DataMember(Order = 4)] public double FromVolume { get; set; }
    }
}
