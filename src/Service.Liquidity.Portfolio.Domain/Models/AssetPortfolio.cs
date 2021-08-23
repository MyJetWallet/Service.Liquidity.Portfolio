using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetPortfolio
    {
        [DataMember(Order = 1)] public List<BalanceByWallet> BalanceByWallet { get; set; }
        [DataMember(Order = 2)] public List<BalanceByAsset> BalanceByAsset { get; set; }

        public AssetPortfolio()
        {
            BalanceByWallet = new List<BalanceByWallet>();
            BalanceByAsset = new List<BalanceByAsset>();
        }
    }
}
