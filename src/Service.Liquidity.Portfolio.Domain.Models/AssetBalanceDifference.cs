namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetBalanceDifference
    {
        public string BrokerId { get; set; }
        public string WalletName { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public double VolumeUsd { get; set; }
        public double CurrentPriceInUsd { get; set; }

        public AssetBalanceDifference(string brokerId, string walletName, string asset, double volume, double volumeUsd, double currentPriceInUsd)
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
