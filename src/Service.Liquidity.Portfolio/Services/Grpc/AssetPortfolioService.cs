using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Domain;
using MyJetWallet.Sdk.Service;
using MyNoSqlServer.Abstractions;
using Newtonsoft.Json;
using Service.AssetsDictionary.Client;
using Service.Liquidity.Engine.Domain.Models.NoSql;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services.Grpc
{
    public class AssetPortfolioService: IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly IAssetPortfolioSettingsStorage _assetPortfolioSettingsStorage;
        private readonly ISpotInstrumentDictionaryClient _spotInstrumentDictionaryClient;
        private readonly IMyNoSqlServerDataReader<LpWalletNoSql> _noSqlDataReader;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            IPortfolioHandler portfolioHandler,
            IAssetPortfolioSettingsStorage assetPortfolioSettingsStorage,
            ISpotInstrumentDictionaryClient spotInstrumentDictionaryClient,
            IMyNoSqlServerDataReader<LpWalletNoSql> noSqlDataReader)
        {
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _portfolioHandler = portfolioHandler;
            _assetPortfolioSettingsStorage = assetPortfolioSettingsStorage;
            _spotInstrumentDictionaryClient = spotInstrumentDictionaryClient;
            _noSqlDataReader = noSqlDataReader;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            _logger.LogInformation("Call GetBalancesAsync");
            var response = new GetBalancesResponse();
            try
            {
                var balancesSnapshot = new List<AssetBalance>();
                // todo: calculate USD for all balances
                
                var internalWallets = _noSqlDataReader.Get().Select(elem => elem.Wallet.Name).ToList();

                response.BalanceByWallet = GetBalanceByWallet(balancesSnapshot, internalWallets);
                response.BalanceByAsset = GetBalanceByAsset(balancesSnapshot, internalWallets);
            }
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
            }
            return response;
        }

        private List<NetBalanceByAsset> GetBalanceByAsset(IReadOnlyCollection<AssetBalance> balancesSnapshot,
            ICollection<string> internalWallets)
        {
            using var a = MyTelemetry.StartActivity("GetBalanceByAsset");
            
            var balanceByAssetCollection = new List<NetBalanceByAsset>();
            
            var assets = balancesSnapshot
                .Select(elem => elem.Asset)
                .Distinct();
            
            var wallets = balancesSnapshot
                .Select(elem => elem.WalletName)
                .Distinct()
                .ToList();

            foreach (var asset in assets)
            {
                var balanceByAsset = new NetBalanceByAsset {Asset = asset, WalletBalances =  new List<NetBalanceByWallet>()};

                foreach (var wallet in wallets)
                {
                    var balanceByWallet = new NetBalanceByWallet()
                    {
                        BrokerId = balancesSnapshot.First(elem => elem.WalletName == wallet).BrokerId,
                        WalletName = wallet
                    };
                    var sumByWallet = balancesSnapshot
                        .Where(elem => elem.WalletName == wallet && elem.Asset == asset)
                        .Sum(elem => elem.Volume);
                    
                    if (sumByWallet != 0)
                    {
                        balanceByWallet.NetVolume = sumByWallet;
                        balanceByWallet.NetUsdVolume = balancesSnapshot
                            .Where(elem => elem.WalletName == wallet && elem.Asset == asset)
                            .Sum(GetUsdProjectionByBalance);
                    }

                    balanceByWallet.IsInternal = internalWallets.Contains(wallet);
                    balanceByAsset.WalletBalances.Add(balanceByWallet);
                }

                var netVolumeByInternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetVolume);
                var netVolumeByExternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => !internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetVolume);
                balanceByAsset.NetVolume = netVolumeByInternalWallets-netVolumeByExternalWallets;

                var netUsdVolumeByInternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetUsdVolume);
                var netUsdVolumeByExternalWallets = balanceByAsset.WalletBalances
                    .Where(elem => !internalWallets.Contains(elem.WalletName))
                    .Sum(elem => elem.NetUsdVolume);
                balanceByAsset.NetUsdVolume = netUsdVolumeByInternalWallets-netUsdVolumeByExternalWallets;

                var assetBalanceSettings = _assetPortfolioSettingsStorage.GetAssetPortfolioSettingsByAsset(asset);
                if (assetBalanceSettings != null)
                {
                    balanceByAsset.Settings = assetBalanceSettings;
                    balanceByAsset.SetState(assetBalanceSettings);
                }
                
                balanceByAssetCollection.Add(balanceByAsset);
            }

            return balanceByAssetCollection;
        }

        private List<NetBalanceByWallet> GetBalanceByWallet(IReadOnlyCollection<AssetBalance> balancesSnapshot,
            ICollection<string> internalWallets)
        {
            using var a = MyTelemetry.StartActivity("GetBalanceByWallet");
            
            var balanceByWallet = balancesSnapshot
                .Select(elem => elem.WalletName)
                .Distinct()
                .Select(walletName => new NetBalanceByWallet() {WalletName = walletName})
                .ToList();

            foreach (var balanceByWalletElem in balanceByWallet)
            {
                var balanceByWalletCollection = balancesSnapshot
                    .Where(assetBalance => balanceByWalletElem.WalletName == assetBalance.WalletName)
                    .ToList();

                balanceByWalletElem.BrokerId = balanceByWalletCollection.First().BrokerId;
                balanceByWalletElem.NetUsdVolume = balanceByWalletCollection
                    .Sum(GetUsdProjectionByBalance);
            }

            balanceByWallet.ForEach(elem =>
            {
                elem.IsInternal = internalWallets.Contains(elem.WalletName);
            });
            
            return balanceByWallet;
        }

        private double GetUsdProjectionByBalance(AssetBalance balance)
        {
            const string projectionAsset = "USD";
            
            var usdProjectionEntity = _anotherAssetProjectionService.GetProjectionAsync(
                new GetProjectionRequest()
                {
                    BrokerId = balance.BrokerId,
                    FromAsset = balance.Asset,
                    FromVolume = balance.Volume,
                    ToAsset = projectionAsset
                }).Result;

            return Math.Round(usdProjectionEntity.ProjectionVolume, 2);
        }

        public async Task<GetChangeBalanceHistoryResponse> GetChangeBalanceHistoryAsync()
        {
            var response = new GetChangeBalanceHistoryResponse();
            try
            {
                response.Histories = await _portfolioHandler.GetHistories();
                response.Success = true;
            }
            catch (Exception exception)
            {
                response.Success = false;
                response.ErrorText = exception.Message;
            }

            return response;
        }

        public async Task<UpdateBalanceResponse> UpdateBalance(UpdateBalanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.WalletName) ||
                string.IsNullOrWhiteSpace(request.Asset) ||
                string.IsNullOrWhiteSpace(request.Comment) ||
                string.IsNullOrWhiteSpace(request.User))
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new UpdateBalanceResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }
            
            if (request.BalanceDifference == 0)
            {
                const string message = "Balance difference is zero.";
                _logger.LogError(message);
                return new UpdateBalanceResponse() {Success = false, ErrorMessage = message};
            }

            try
            {
                var updateDate = DateTime.UtcNow;
                var newBalance = new AssetBalance()
                {
                    BrokerId = request.BrokerId,
                    Asset = request.Asset,
                    UpdateDate = updateDate,
                    Volume = request.BalanceDifference,
                    WalletName = request.WalletName
                };
                _portfolioHandler.UpdateBalance(new List<AssetBalance>() {newBalance});
                await _portfolioHandler.SaveChangeBalanceHistoryAsync(new ChangeBalanceHistory()
                {
                    Asset = request.Asset,
                    BrokerId = request.BrokerId,
                    Comment = request.Comment,
                    UpdateDate = updateDate,
                    User = request.User,
                    VolumeDifference = request.BalanceDifference,
                    WalletName = request.WalletName
                });
            }
            catch (Exception exception)
            {
                _logger.LogError($"Update failed: {JsonConvert.SerializeObject(exception)}");
                return new UpdateBalanceResponse() {Success = false, ErrorMessage = exception.Message};
            }

            return new UpdateBalanceResponse() {Success = true};
        }
        }
}
