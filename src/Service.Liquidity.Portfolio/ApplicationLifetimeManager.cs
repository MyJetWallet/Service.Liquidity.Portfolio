using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.NoSql;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.DataReader;
using MyServiceBus.TcpClient;
using Service.Liquidity.Portfolio.Jobs;

namespace Service.Liquidity.Portfolio
{
    public class ApplicationLifetimeManager : ApplicationLifetimeManagerBase
    {
        private readonly ILogger<ApplicationLifetimeManager> _logger;
        private readonly MyServiceBusTcpClient _myServiceBusTcpClient;
        private readonly MyNoSqlTcpClient _myNoSqlTcpClient;
        private readonly BalancePersistJob _balancePersistJob;
        private readonly MyNoSqlClientLifeTime _myNoSqlClientLifeTime;
        
        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime,
            ILogger<ApplicationLifetimeManager> logger,
            MyServiceBusTcpClient myServiceBusTcpClient,
            MyNoSqlTcpClient myNoSqlTcpClient,
            BalancePersistJob balancePersistJob,
            MyNoSqlClientLifeTime myNoSqlClientLifeTime)
            : base(appLifetime)
        {
            _logger = logger;
            _myServiceBusTcpClient = myServiceBusTcpClient;
            _myNoSqlTcpClient = myNoSqlTcpClient;
            _balancePersistJob = balancePersistJob;
            _myNoSqlClientLifeTime = myNoSqlClientLifeTime;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _balancePersistJob.Start();
            _myNoSqlTcpClient.Start();
            _myServiceBusTcpClient.Start();
            _myNoSqlClientLifeTime.Start();
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myNoSqlTcpClient.Stop();
            _myNoSqlClientLifeTime.Stop();
            try
            {
                _myServiceBusTcpClient.Stop();
                _balancePersistJob.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception on MyServiceBusTcpClient.Stop: {ex}");
            }
        }

        protected override void OnStopped()
        {
            _logger.LogInformation("OnStopped has been called.");
        }
    }
}
