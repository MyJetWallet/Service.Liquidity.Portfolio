using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class UpdateBalanceRequest
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string WalletName { get; set; }
        [DataMember(Order = 3)] public string Asset { get; set; }
        [DataMember(Order = 4)] public decimal BalanceDifference { get; set; }
        [DataMember(Order = 5)] public string Comment { get; set; }
        [DataMember(Order = 6)] public string User { get; set; }
    }
}
