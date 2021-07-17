using MyNoSqlServer.Abstractions;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetPortfolioBalanceNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "jetwallet-liquidity-portfolio";
        private static string GeneratePartitionKey() => $"balance";
        private static string GenerateRowKey() => $"balance";

        public AssetPortfolio Balance { get; set; }
        
        public static AssetPortfolioBalanceNoSql Create(AssetPortfolio balance)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(),
                Balance = balance
            };
        }
    }
}
