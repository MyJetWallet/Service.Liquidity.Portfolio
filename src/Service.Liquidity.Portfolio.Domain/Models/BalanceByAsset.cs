using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class BalanceByAsset
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public List<BalanceByWallet> WalletBalances { get; set; }
        [DataMember(Order = 4)] public decimal Volume { get; set; }
        [DataMember(Order = 5)] public decimal UsdVolume { get; set; }
        [DataMember(Order = 6)] public decimal OpenPriceAvg { get; set; }
        [DataMember(Order = 7)] public decimal UnrealisedPnl { get; set; }

        public BalanceByAsset()
        {
            WalletBalances = new List<BalanceByWallet>();
        }

        public BalanceByAsset GetCopy()
        {
            var netBalance =  new BalanceByAsset()
            {
                Asset = Asset,
                Volume = Volume,
                UsdVolume = UsdVolume,
                OpenPriceAvg = OpenPriceAvg,
                UnrealisedPnl = UnrealisedPnl,
                WalletBalances = new List<BalanceByWallet>()
            };

            if (WalletBalances != null && WalletBalances.Any())
                netBalance.WalletBalances.AddRange(WalletBalances.Select(e => e.GetCopy()).ToList());
                    
            return netBalance;
        }
    }
}
