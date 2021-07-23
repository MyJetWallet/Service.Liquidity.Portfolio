using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class PnlByAsset
    {
        [DataMember(Order = 1)] public string Asset { get; set; }
        [DataMember(Order = 2)] public decimal Pnl { get; set; }

        public static PnlByAsset Create(string asset, decimal pnl)
        {
            return new PnlByAsset() {Asset = asset, Pnl = pnl};
        }
    }
}
