using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class UpdateBalanceRequest
    {
        [DataMember(Order = 1)] public AssetBalanceGrpc AssetBalance { get; set; }
        [DataMember(Order = 2)] public double BalanceDifference { get; set; }
        [DataMember(Order = 3)] public string Comment { get; set; }
        [DataMember(Order = 4)] public string User { get; set; }
    }
}
