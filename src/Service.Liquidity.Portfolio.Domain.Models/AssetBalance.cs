using System;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetBalance
    {
        public string BrokerId { get; set; }
        public string ClientId { get; set; }
        public string WalletId { get; set; }
        public string Asset { get; set; }
        public double Volume { get; set; }
        public DateTime UpdateDate { get; set; }
        
        public AssetBalance()
        {
        }
    }
}
