using MyJetWallet.Sdk.Postgres;

namespace Service.Liquidity.Portfolio.Postgres.DesignTime
{
    public class ContextFactory : MyDesignTimeContextFactory<TradeContext>
    {
        public ContextFactory() : base(options => new TradeContext(options))
        {
        }
    }
}
