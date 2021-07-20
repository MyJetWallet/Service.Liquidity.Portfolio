namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetBalanceDifference
    {
        public string BrokerId { get; set; }
        public string WalletName { get; set; }
        public string Asset { get; set; }
        public decimal Volume { get; set; }
        public decimal VolumeUsd { get; set; }
        public decimal CurrentPriceInUsd { get; set; }

        public AssetBalanceDifference(string brokerId, string walletName, string asset, decimal volume, decimal volumeUsd, decimal currentPriceInUsd)
        {
            BrokerId = brokerId;
            WalletName = walletName;
            Asset = asset;
            Volume = volume;
            VolumeUsd = volumeUsd;
            CurrentPriceInUsd = currentPriceInUsd;
        }
    }
}
