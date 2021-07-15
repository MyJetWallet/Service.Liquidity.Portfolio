using System;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class AssetPortfolioTrade
    {
        public const string TopicName = "jetwallet-liquidity-portfolio-trades";
        [DataMember(Order = 1)] public long Id { get; set; }
        [DataMember(Order = 2)] public string TradeId { get; set; }
        [DataMember(Order = 3)] public string AssociateBrokerId { get; set; }
        [DataMember(Order = 4)] public string WalletName { get; set; }
        [DataMember(Order = 5)] public string AssociateSymbol { get; set; }
        [DataMember(Order = 6)] public string BaseAsset { get; set; }
        [DataMember(Order = 7)] public string QuoteAsset { get; set; }
        [DataMember(Order = 8)] public OrderSide Side { get; set; }
        [DataMember(Order = 9)] public double Price { get; set; }
        [DataMember(Order = 10)] public double BaseVolume { get; set; }
        [DataMember(Order = 11)] public double QuoteVolume { get; set; }
        [DataMember(Order = 12)] public double BaseVolumeInUsd { get; set; }
        [DataMember(Order = 13)] public double QuoteVolumeInUsd { get; set; }
        [DataMember(Order = 14)] public DateTime DateTime { get; set; }
        [DataMember(Order = 15)] public string ErrorMessage { get; set; }
        [DataMember(Order = 16)] public string Source { get; set; }
        [DataMember(Order = 17)] public string Comment { get; set; }
        [DataMember(Order = 18)] public string User { get; set; }

        public AssetPortfolioTrade(string tradeId, 
            string associateBrokerId,
            string associateSymbol,
            string baseAsset,
            string quoteAsset,
            string walletName,
            OrderSide side, double price, double baseVolume,
            double quoteVolume, DateTime dateTime, string source)
        {
            TradeId = tradeId;
            AssociateBrokerId = associateBrokerId;
            WalletName = walletName;
            AssociateSymbol = associateSymbol;
            BaseAsset = baseAsset;
            QuoteAsset = quoteAsset;
            Side = side;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            DateTime = dateTime;
            Source = source;
        }

        public AssetPortfolioTrade(string associateBrokerId,
            string associateSymbol,
            string baseAsset,
            string quoteAsset,
            string walletName,
            double price, double baseVolume,
            double quoteVolume, string comment, string user, string source)
        {
            AssociateBrokerId = associateBrokerId;
            WalletName = walletName;
            AssociateSymbol = associateSymbol;
            BaseAsset = baseAsset;
            QuoteAsset = quoteAsset;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            Source = source;
            Comment = comment;
            User = user;
            
            TradeId = Guid.NewGuid().ToString("N");
            DateTime = DateTime.UtcNow;
            Side = baseVolume < 0 ? OrderSide.Sell : OrderSide.Buy;
        }

        public AssetPortfolioTrade()
        {
        }
    }
}
