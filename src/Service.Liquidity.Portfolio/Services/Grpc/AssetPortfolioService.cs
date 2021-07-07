using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.Liquidity.Portfolio.Domain.Models;
using Service.Liquidity.Portfolio.Domain.Services;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.Liquidity.Portfolio.Grpc.Models.GetBalances;
using Service.Liquidity.Portfolio.Postgres;

namespace Service.Liquidity.Portfolio.Services.Grpc
{
    public class AssetPortfolioService: IAssetPortfolioService
    {
        private readonly ILogger<AssetPortfolioService> _logger;
        private readonly IAnotherAssetProjectionService _anotherAssetProjectionService;
        private readonly IPortfolioHandler _portfolioHandler;
        private readonly IAssetPortfolioSettingsStorage _assetPortfolioSettingsStorage;

        public AssetPortfolioService(ILogger<AssetPortfolioService> logger,
            IAnotherAssetProjectionService anotherAssetProjectionService,
            IPortfolioHandler portfolioHandler,
            IAssetPortfolioSettingsStorage assetPortfolioSettingsStorage)
        {
            _logger = logger;
            _anotherAssetProjectionService = anotherAssetProjectionService;
            _portfolioHandler = portfolioHandler;
            _assetPortfolioSettingsStorage = assetPortfolioSettingsStorage;
        }

        public async Task<GetBalancesResponse> GetBalancesAsync()
        {
            var response = new GetBalancesResponse();
            try
            {
                var balancesSnapshot = _portfolioHandler.GetBalancesSnapshot();
                
                response.BalanceByWallet = GetBalanceByWallet(balancesSnapshot);
                response.BalanceByAsset = GetBalanceByAsset(balancesSnapshot);
            }
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
            }
            return response;
        }

        private List<NetBalanceByAsset> GetBalanceByAsset(List<AssetBalance> balancesSnapshot)
        {
            var balanceByAssetCollection = new List<NetBalanceByAsset>();
            
            var assets = balancesSnapshot
                .Select(elem => elem.Asset)
                .Distinct();
            
            var wallets = balancesSnapshot
                .Select(elem => elem.WalletName)
                .Distinct();

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
                    balanceByAsset.WalletBalances.Add(balanceByWallet);
                }

                balanceByAsset.NetVolume = balanceByAsset.WalletBalances.Sum(elem => elem.NetVolume);
                balanceByAsset.NetUsdVolume = balanceByAsset.WalletBalances.Sum(elem => elem.NetUsdVolume);

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

        private List<NetBalanceByWallet> GetBalanceByWallet(IReadOnlyCollection<AssetBalance> balancesSnapshot)
        {
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

        public async Task<GetTradesResponse> GetTradesAsync(GetTradesRequest request)
        {
           
            var response = new GetTradesResponse();
            try
            {
                var trades = await _portfolioHandler.GetTrades(request.LastId, request.BatchSize, request.AssetFilter);

                long idForNextQuery = 0;
                if (trades.Any())
                {
                    idForNextQuery = trades.Select(elem => elem.Id).Min();
                }

                response.Success = true;
                response.Trades = trades;
                response.IdForNextQuery = idForNextQuery;
            } 
            catch (Exception exception)
            {
                _logger.LogError(JsonConvert.SerializeObject(exception));
                
                response.Success = false;
                response.ErrorMessage = exception.Message;
            }

            return response;
        }

        public async Task<CreateTradeManualResponse> CreateManualTradeAsync(CreateTradeManualRequest request)
        {
            
            using var activity = MyTelemetry.StartActivity("CreateManualTradeAsync");

            request.AddToActivityAsJsonTag("CreateTradeManualRequest");
            
            _logger.LogInformation($"CreateManualTradeAsync receive request: {JsonConvert.SerializeObject(request)}");
            
            if (string.IsNullOrWhiteSpace(request.BrokerId) ||
                string.IsNullOrWhiteSpace(request.WalletName) ||
                string.IsNullOrWhiteSpace(request.Symbol) ||
                string.IsNullOrWhiteSpace(request.Comment) ||
                string.IsNullOrWhiteSpace(request.User) ||
                request.Price == 0 ||
                request.BaseVolume == 0 ||
                request.QuoteVolume == 0 ||
                (request.BaseVolume > 0 && request.QuoteVolume > 0) ||
                (request.BaseVolume < 0 && request.QuoteVolume < 0))
            {
                _logger.LogError($"Bad request entity: {JsonConvert.SerializeObject(request)}");
                return new CreateTradeManualResponse() {Success = false, ErrorMessage = "Incorrect entity"};
            }

            var trade = new Trade(request.BrokerId, request.WalletName,
                request.Symbol, request.Price, request.BaseVolume,
                request.QuoteVolume, request.Comment, request.User, "manual");
            try
            {
                await _portfolioHandler.HandleTradesAsync(new List<Trade>() {trade});
            }
            catch (Exception exception)
            {
                _logger.LogError($"Creating failed: {JsonConvert.SerializeObject(exception)}");
                return new CreateTradeManualResponse() {Success = false, ErrorMessage = exception.Message};
            }

            var response = new CreateTradeManualResponse() {Success = true, Trade = trade};
            
            _logger.LogInformation($"CreateManualTradeAsync return reponse: {JsonConvert.SerializeObject(response)}");
            return response;
        }
    }
}
