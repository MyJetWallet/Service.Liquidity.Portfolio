using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Grpc.Models
{
    [DataContract]
    public class NetBalanceByWallet
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string WalletName { get; set; }
        [DataMember(Order = 3)] public double NetVolume { get; set; }
        [DataMember(Order = 4)] public double NetUsdVolume { get; set; }
        [DataMember(Order = 5)] public bool IsInternal { get; set; }
    }
}
