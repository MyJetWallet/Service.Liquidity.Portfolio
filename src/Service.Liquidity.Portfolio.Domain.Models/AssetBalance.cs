using System;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetBalance
    {
        public string BrokerId { get; set; }
        public string WalletName { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        
        public AssetBalance()
        {
        }

        public AssetBalance Copy()
        {
            return new AssetBalance()
            {
                BrokerId = this.BrokerId,
                WalletName = this.WalletName,
                Asset = this.Asset,
                Volume = this.Volume
            };
        }
    }
}
