using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using MyNoSqlServer.Abstractions;
using Service.Liquidity.Portfolio.Domain.Models.NoSql;

namespace Service.Liquidity.Portfolio.Services
{
    public class FeeShareOperationCache : IStartable
    {
        private readonly IMyNoSqlServerDataWriter<FeeShareOperationNoSqlEntity> _operationWriter;
        private readonly HashSet<string> _operations = new HashSet<string>();
        public FeeShareOperationCache(IMyNoSqlServerDataWriter<FeeShareOperationNoSqlEntity> operationWriter)
        {
            _operationWriter = operationWriter;
        }

        public void Start()
        {
            var list = _operationWriter.GetAsync().Result.ToList();
            foreach (var entity in list)
            {
                _operations.Add(entity.OperationId);
            }
        }

        public async Task<bool> WasRecorded(string operationId)
        {
            if (_operations.TryGetValue(operationId, out operationId))
                return true;

            _operations.Add(operationId);
            await _operationWriter.InsertOrReplaceAsync(FeeShareOperationNoSqlEntity.Create(operationId));
            await _operationWriter.CleanAndKeepLastRecordsAsync(FeeShareOperationNoSqlEntity.GeneratePartitionKey(),
                1000);
            
            return false;
        }
    }
}
