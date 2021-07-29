using System;
using System.Runtime.Serialization;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetBalance
    {
        [DataMember(Order = 1)]
        public string BrokerId { get; set; }
        
        [DataMember(Order = 2)]
        public string WalletName { get; set; }
        
        [DataMember(Order = 3)]
        public string Asset { get; set; }
        
        [DataMember(Order = 4)]
        public decimal Volume { get; set; }
        
        [DataMember(Order = 5)]
        public decimal OpenPrice { get; set; }

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
                Volume = this.Volume,
                OpenPrice = this.OpenPrice
            };
        }
    }
}
