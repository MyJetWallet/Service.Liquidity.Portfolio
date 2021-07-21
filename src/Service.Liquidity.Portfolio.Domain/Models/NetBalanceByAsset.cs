using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class NetBalanceByAsset
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public List<NetBalanceByWallet> WalletBalances { get; set; }
        [DataMember(Order = 4)] public decimal NetVolume { get; set; }
        [DataMember(Order = 5)] public decimal NetUsdVolume { get; set; }
        [DataMember(Order = 6)] public decimal OpenPriceAvg { get; set; }
        [DataMember(Order = 7)] public decimal UnrealisedPnl { get; set; }
        
        public NetBalanceByAsset()
        {
        }
    }
}
