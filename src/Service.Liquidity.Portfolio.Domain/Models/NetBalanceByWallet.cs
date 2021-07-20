using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class NetBalanceByWallet
    {
        [DataMember(Order = 1)] public string BrokerId { get; set; }
        [DataMember(Order = 2)] public string WalletName { get; set; }
        [DataMember(Order = 3)] public decimal NetVolume { get; set; }
        [DataMember(Order = 4)] public decimal NetUsdVolume { get; set; }
        [DataMember(Order = 5)] public bool IsInternal { get; set; }
    }
}
