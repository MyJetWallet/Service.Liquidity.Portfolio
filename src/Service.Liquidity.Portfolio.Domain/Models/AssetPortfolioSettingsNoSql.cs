using MyNoSqlServer.Abstractions;

namespace Service.Liquidity.Portfolio.Domain.Models
{
    public class AssetPortfolioSettingsNoSql : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-liquitityportfolio-settings";
        private static string GeneratePartitionKey(string asset) => $"asset:{asset}";
        private static string GenerateRowKey() => "settings";
        
        public AssetPortfolioSettings Settings { get; set; }
        
        public static AssetPortfolioSettingsNoSql Create(AssetPortfolioSettings settings)
        {
            return new()
            {
                PartitionKey = GeneratePartitionKey(settings.Asset),
                RowKey = GenerateRowKey(),
                Settings = settings
            };
        }
    }
}
