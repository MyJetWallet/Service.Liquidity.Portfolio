using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly MyNoSqlTcpClient[] _myNoSqlTcpClientManagers;
        private readonly BalancePersistJob _balancePersistJob;
        
        public ApplicationLifetimeManager(IHostApplicationLifetime appLifetime,
            ILogger<ApplicationLifetimeManager> logger,
            MyServiceBusTcpClient myServiceBusTcpClient,
            MyNoSqlTcpClient myNoSqlTcpClient,
            MyNoSqlTcpClient[] myNoSqlTcpClientManagers,
            BalancePersistJob balancePersistJob)
            : base(appLifetime)
        {
            _logger = logger;
            _myServiceBusTcpClient = myServiceBusTcpClient;
            _myNoSqlTcpClient = myNoSqlTcpClient;
            _myNoSqlTcpClientManagers = myNoSqlTcpClientManagers;
            _balancePersistJob = balancePersistJob;
        }

        protected override void OnStarted()
        {
            _logger.LogInformation("OnStarted has been called.");
            _myNoSqlTcpClient.Start();
            _myServiceBusTcpClient.Start();
            _balancePersistJob.Start();
            
            foreach(var client in _myNoSqlTcpClientManagers)
            {
                client.Start();
            }
        }

        protected override void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
            _myNoSqlTcpClient.Stop();
            
            foreach(var client in _myNoSqlTcpClientManagers)
            {
                try
                {
                    client.Start();
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            
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
