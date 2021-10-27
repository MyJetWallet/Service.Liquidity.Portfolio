using MyNoSqlServer.Abstractions;

namespace Service.Liquidity.Portfolio.Domain.Models.NoSql
{
    public class FeeShareOperationNoSqlEntity : MyNoSqlDbEntity
    {
        public const string TableName = "myjetwallet-portfolio-feeshares";

        public static string GeneratePartitionKey() => "FeeShareOperations";

        public static string GenerateRowKey(string operationId) => operationId;
        
        public string OperationId { get; set; }
        
        public static FeeShareOperationNoSqlEntity Create(string operationId) =>
            new()
            {
                PartitionKey = GeneratePartitionKey(),
                RowKey = GenerateRowKey(operationId),
                OperationId = operationId
            };
    }
}
