using System;
using System.Runtime.Serialization;
using Service.BalanceHistory.Domain.Models;
using MyJetWallet.Domain.Orders;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    [DataContract]
    public class PortfolioTrade
    {
        public const string TopicName = "spot-liquidity-engine-trade";

        [DataMember(Order = 1)] public string TradeId { get; set; }
        [DataMember(Order = 2)] public string Source { get; set; }
        [DataMember(Order = 3)] public bool IsInternal { get; set; }
        [DataMember(Order = 4)] public string Symbol { get; set; }
        [DataMember(Order = 5)] public OrderSide Side { get; set; }
        [DataMember(Order = 6)] public double Price { get; set; }
        [DataMember(Order = 7)] public double BaseVolume { get; set; }
        [DataMember(Order = 8)] public double QuoteVolume { get; set; }
        [DataMember(Order = 9)] public DateTime DateTime { get; set; }
        [DataMember(Order = 10)] public string ReferenceId { get; set; }

        public PortfolioTrade(WalletTrade trade, string walletId)
        {
            Source = walletId;
            IsInternal = true;
            Symbol = trade.InstrumentSymbol;
            Price = trade.Price;
            BaseVolume = trade.BaseVolume;
            QuoteVolume = trade.QuoteVolume;
            DateTime = trade.DateTime;
            TradeId = trade.TradeUId;
            Side = trade.Side;
            ReferenceId = string.Empty;
        }

        public PortfolioTrade(string tradeId, string source, bool isInternal, string symbol, OrderSide side,
            double price, double baseVolume,
            double quoteVolume, DateTime dateTime, string referenceId)
        {
            TradeId = tradeId;
            Source = source;
            IsInternal = isInternal;
            Symbol = symbol;
            Side = side;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            DateTime = dateTime;
            ReferenceId = referenceId;
        }

        public PortfolioTrade()
        {
        }
    }
}
