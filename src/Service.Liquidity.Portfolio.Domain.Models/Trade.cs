using System;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class Trade
    {
        [DataMember(Order = 1)] public long Id { get; set; }
        [DataMember(Order = 2)] public string TradeId { get; set; }
        [DataMember(Order = 3)] public string BrokerId { get; set; }
        [DataMember(Order = 4)] public string ClientId { get; set; }
        [DataMember(Order = 5)] public string WalletId { get; set; }
        [DataMember(Order = 6)] public string Symbol { get; set; }
        [DataMember(Order = 7)] public OrderSide Side { get; set; }
        [DataMember(Order = 8)] public double Price { get; set; }
        [DataMember(Order = 9)] public double BaseVolume { get; set; }
        [DataMember(Order = 10)] public double QuoteVolume { get; set; }
        [DataMember(Order = 11)] public double BaseVolumeInUsd { get; set; }
        [DataMember(Order = 12)] public double QuoteVolumeInUsd { get; set; }
        [DataMember(Order = 13)] public DateTime DateTime { get; set; }
        [DataMember(Order = 14)] public string ErrorMessage { get; set; }
        [DataMember(Order = 15)] public string Source { get; set; }

        public Trade(string tradeId, string brokerId, string clientId, string walletId, string symbol,
            OrderSide side, double price, double baseVolume,
            double quoteVolume, DateTime dateTime, string source)
        {
            TradeId = tradeId;
            BrokerId = brokerId;
            ClientId = clientId;
            WalletId = walletId;
            Symbol = symbol;
            Side = side;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            DateTime = dateTime;
            Source = source;
        }

        public Trade(string brokerId, string clientId, string walletId,
            string symbol, double price, double baseVolume,
            double quoteVolume, string source)
        {
            BrokerId = brokerId;
            ClientId = clientId;
            WalletId = walletId;
            Symbol = symbol;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            Source = source;
            
            TradeId = Guid.NewGuid().ToString("N");
            DateTime = DateTime.UtcNow;
            Side = baseVolume < 0 ? OrderSide.Sell : OrderSide.Buy;
        }

        public Trade()
        {
        }
    }
}
