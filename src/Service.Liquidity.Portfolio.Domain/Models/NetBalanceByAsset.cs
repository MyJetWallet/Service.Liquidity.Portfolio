using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class NetBalanceByAsset
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public List<NetBalanceByWallet> WalletBalances { get; set; }
        [DataMember(Order = 4)] public double NetVolume { get; set; }
        [DataMember(Order = 5)] public double NetUsdVolume { get; set; }
        
        public NetBalanceByAsset()
        {
        }
    }
}
