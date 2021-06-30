using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using MyJetWallet.Domain.Orders;
using Service.BalanceHistory.Domain.Models;

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

        public Trade(WalletTrade trade, string walletId)
        {
            WalletId = walletId;
            Symbol = trade.InstrumentSymbol;
            Price = trade.Price;
            BaseVolume = trade.BaseVolume;
            QuoteVolume = trade.QuoteVolume;
            DateTime = trade.DateTime;
            TradeId = trade.TradeUId;
            Side = trade.Side;
        }

        public Trade(string tradeId, string walletId, string symbol, OrderSide side,
            double price, double baseVolume,
            double quoteVolume, DateTime dateTime)
        {
            TradeId = tradeId;
            WalletId = walletId;
            Symbol = symbol;
            Side = side;
            Price = price;
            BaseVolume = baseVolume;
            QuoteVolume = quoteVolume;
            DateTime = dateTime;
        }

        public Trade()
        {
        }
    }
}
