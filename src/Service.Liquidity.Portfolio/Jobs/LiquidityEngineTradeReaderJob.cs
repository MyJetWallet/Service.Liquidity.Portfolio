using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using MyJetWallet.Domain;
using MyJetWallet.Domain.Orders;
using Service.AssetsDictionary.Client;
using Service.Liquidity.Engine.Domain.Models.Portfolio;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class LiquidityEngineTradeReaderJob : IStartable
    {
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        public LiquidityEngineTradeReaderJob(ISubscriber<IReadOnlyList<PortfolioTrade>> subscriber,
            IPortfolioHandler portfolioHandler, 
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient)
        {
            _portfolioHandler = portfolioHandler;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            subscriber.Subscribe(HandleTrades);
        }

        private async ValueTask HandleTrades(IReadOnlyList<PortfolioTrade> trades)
        {
            var localTrades = new List<AssetPortfolioTrade>();
            foreach (var elem in trades.Where(elem => !elem.IsInternal))
            {
                var instruments = _spotInstrumentDictionaryClient.GetSpotInstrumentByBroker(new JetBrandIdentity
                {
                    BrokerId = elem.AssociateBrokerId
                });
                var instrument = instruments.FirstOrDefault(e => e.Symbol == elem.AssociateSymbol);
                
                localTrades.Add(new AssetPortfolioTrade(elem.TradeId,
                    elem.AssociateBrokerId,
                    elem.AssociateSymbol,
                    instrument?.BaseAsset,
                    instrument?.QuoteAsset,
                    elem.Source,
                    elem.Side,
                    elem.Price,
                    elem.Side == OrderSide.Buy ? elem.BaseVolume : -elem.BaseVolume,
                    elem.Side == OrderSide.Buy ? -elem.QuoteVolume : elem.QuoteVolume,
                    elem.DateTime,
                    PortfolioTrade.TopicName));
            }
            await _portfolioHandler.HandleTradesAsync(localTrades);
        }

        public void Start()
        {
        }
    }
}
