using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetPortfolio
    {
        [DataMember(Order = 1)] public List<NetBalanceByWallet> BalanceByWallet { get; set; }
        [DataMember(Order = 2)] public List<NetBalanceByAsset> BalanceByAsset { get; set; }

        public AssetPortfolio()
        {
            BalanceByWallet = new List<NetBalanceByWallet>();
            BalanceByAsset = new List<NetBalanceByAsset>();
        }
    }
}
