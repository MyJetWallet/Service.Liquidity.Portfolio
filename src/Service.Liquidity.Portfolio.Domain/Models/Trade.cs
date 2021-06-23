using Service.BalanceHistory.Domain.Models;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class Trade : WalletTrade
    {
        public string WalletId { get; set; }

        public Trade(WalletTrade walletTrade, string walletId) : base(walletTrade)
        {
            WalletId = walletId;
        }

        public Trade()
        {
            
        }
    }
}
