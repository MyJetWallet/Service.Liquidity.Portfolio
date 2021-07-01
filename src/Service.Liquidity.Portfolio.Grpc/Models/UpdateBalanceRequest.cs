using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class UpdateBalanceRequest
    {
        [DataMember(Order = 1)] public AssetBalanceGrpc AssetBalance { get; set; }
    }
}
