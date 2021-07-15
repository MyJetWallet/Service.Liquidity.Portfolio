using System;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using DotNetCoreDecorators;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service.Tools;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models.GetBalances;

namespace Service.Liquidity.Portfolio.Jobs
{
    public class AssetBalanceWriterJob : IStartable
    {
        private readonly ILogger<AssetBalanceWriterJob> _logger;
        private readonly MyTaskTimer _timer;
        private readonly IAssetPortfolioService _assetPortfolioService;
        
        private GetBalancesResponse LastBalancesCache { get; set; }

        public AssetBalanceWriterJob(ILogger<AssetBalanceWriterJob> logger,
            IAssetPortfolioService assetPortfolioService)
        {
            _logger = logger;
            _assetPortfolioService = assetPortfolioService;
            _timer = new MyTaskTimer(nameof(BalancePersistJob), TimeSpan.FromSeconds(Program.Settings.AssetBalancePublisherTimeInSecond), _logger, DoTime);
            Console.WriteLine($"AssetBalanceWriterJob timer: {TimeSpan.FromSeconds(Program.Settings.AssetBalancePublisherTimeInSecond)}");
        }

        private async Task DoTime()
        {
            await HandleBalances();
        }

        private async Task HandleBalances()
        {
            var actualBalances = await _assetPortfolioService.GetBalancesAsync();
            foreach (var balanceByAsset in actualBalances.BalanceByAsset)
            {
                var lastBalanceInfo =
                    LastBalancesCache.BalanceByAsset.FirstOrDefault(elem => elem.Asset == balanceByAsset.Asset);
                
                if (lastBalanceInfo == null || lastBalanceInfo.BalanceState != balanceByAsset.BalanceState)
                {
                    await PublishBalance(balanceByAsset);
                }
            }
            LastBalancesCache = actualBalances;
        }

        private async Task PublishBalance(NetBalanceByAsset balanceByAsset)
        {
        }

        private async Task SetActualBalances()
        {
            LastBalancesCache = await _assetPortfolioService.GetBalancesAsync();
        }

        public void Start()
        {
            SetActualBalances().GetAwaiter().GetResult();
            _timer.Start();
        }
    }
}
