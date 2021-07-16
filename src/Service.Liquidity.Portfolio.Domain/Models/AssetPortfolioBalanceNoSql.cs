using MyNoSqlServer.Abstractions;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetPortfolioBalanceNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "jetwallet-liquidity-portfolio";
        private static string GeneratePartitionKey(string walletName) => $"walletName:{walletName}";
        private static string GenerateRowKey(string asset) => $"asset:{asset}";

        public AssetBalance Balance { get; set; }
        
        public static AssetPortfolioBalanceNoSql Create(AssetBalance balance)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(balance.WalletName),
                RowKey = GenerateRowKey(balance.Asset),
                Balance = balance
            };
        }
    }
}
