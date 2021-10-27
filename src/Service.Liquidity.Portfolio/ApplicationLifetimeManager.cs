using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyJetWallet.Sdk.ServiceBus;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;
using Service.Liquidity.Portfolio.Jobs;
using Service.Liquidity.Portfolio.Services;

namespace Service.Liquidity.Portfolio
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly ServiceBusLifeTime _myServiceBusTcpClient;
        private readonly MyNoSqlTcpClient _myNoSqlTcpClient;
        private readonly BalancePersistJob _balancePersistJob;
        private readonly MyNoSqlClientLifeTime _myNoSqlClientLifeTime;
        private readonly FeeShareOperationCache _feeShareOperationCache;
        
        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime,
            ILogger<ApplicationLifetimeManager> logger,
            ServiceBusLifeTime myServiceBusTcpClient,
            MyNoSqlTcpClient myNoSqlTcpClient,
            BalancePersistJob balancePersistJob,
            MyNoSqlClientLifeTime myNoSqlClientLifeTime, FeeShareOperationCache feeShareOperationCache)
            : base(appLifetime)
        {
            _logger = logger;
            _myServiceBusTcpClient = myServiceBusTcpClient;
            _myNoSqlTcpClient = myNoSqlTcpClient;
            _balancePersistJob = balancePersistJob;
            _myNoSqlClientLifeTime = myNoSqlClientLifeTime;
            _feeShareOperationCache = feeShareOperationCache;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _balancePersistJob.Start().GetAwaiter().GetResult();
            _myNoSqlTcpClient.Start();
            _myServiceBusTcpClient.Start();
            _myNoSqlClientLifeTime.Start();
            _feeShareOperationCache.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myNoSqlTcpClient.Stop();
            _myNoSqlClientLifeTime.Stop();
            try
            {
                _myServiceBusTcpClient.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception on MyServiceBusTcpClient.Stop: {ex}");
            }
            
            try
            {
                _balancePersistJob.Stop().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception on BalancePersistJob.Stop");
                Console.WriteLine($"Exception on BalancePersistJob.Stop: {ex}");
            }
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
