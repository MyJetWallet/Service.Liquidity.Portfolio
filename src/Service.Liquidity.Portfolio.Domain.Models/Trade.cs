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
        [DataMember(Order = 3)] public string WalletId { get; set; }
        [DataMember(Order = 4)] public string Symbol { get; set; }
        [DataMember(Order = 5)] public OrderSide Side { get; set; }
        [DataMember(Order = 6)] public double Price { get; set; }
        [DataMember(Order = 7)] public double BaseVolume { get; set; }
        [DataMember(Order = 8)] public double QuoteVolume { get; set; }
        [DataMember(Order = 9)] public DateTime DateTime { get; set; }
        [DataMember(Order = 10)] public string TopicSource { get; set; }

        public Trade(string tradeId, string walletId, string symbol,
            OrderSide side, double price, double baseVolume,
            double quoteVolume, DateTime dateTime, string topicSource)
        {
            TradeId = tradeId;
            WalletId = walletId;
            Symbol = symbol;
            Side = side;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            DateTime = dateTime;
            TopicSource = topicSource;
        }

        public Trade()
        {
        }
    }
}
