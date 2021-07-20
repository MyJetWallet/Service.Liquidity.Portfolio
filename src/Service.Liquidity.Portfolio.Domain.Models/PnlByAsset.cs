using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class PnlByAsset
    {
        public long Id { get; set; }
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public decimal Pnl { get; set; }
        public AssetPortfolioTrade Trade { get; set; }

        public static PnlByAsset Create(string asset, decimal pnl, AssetPortfolioTrade trade)
        {
            return new PnlByAsset() {Asset = asset, Pnl = pnl, Trade = trade};
        }
    }
}
