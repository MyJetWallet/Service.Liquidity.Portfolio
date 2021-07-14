using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyJetWallet.Sdk.Service;
using Newtonsoft.Json;
using Service.BaseCurrencyConverter.Grpc;
using Service.BaseCurrencyConverter.Grpc.Models;
using Service.Liquidity.Portfolio.Grpc;
using Service.Liquidity.Portfolio.Grpc.Models;
using Service.MatchingEngine.PriceSource.Client;

namespace Service.Liquidity.Portfolio.Services.Grpc
{
    public class AnotherAssetProjectionService : IAnotherAssetProjectionService
    {
        private readonly IBaseCurrencyConverterService _baseCurrencyConverterService;
        private readonly ICurrentPricesCache _currentPricesCache;
        private readonly ILogger<AnotherAssetProjectionService> _logger;

        public AnotherAssetProjectionService(IBaseCurrencyConverterService baseCurrencyConverterService,
            ICurrentPricesCache currentPricesCache, ILogger<AnotherAssetProjectionService> logger)
        {
            _baseCurrencyConverterService = baseCurrencyConverterService;
            _currentPricesCache = currentPricesCache;
            _logger = logger;
        }

        public async Task<GetProjectionResponse> GetProjectionAsync(GetProjectionRequest request)
        {
            request.AddToActivityAsJsonTag("GetProjectionRequest");
            _logger.LogInformation($"Receive GetProjectionRequest: {JsonConvert.SerializeObject(request)}");
            
            GetProjectionResponse response;
            
            if (request.FromAsset == request.ToAsset || request.FromVolume == 0)
            {
                response = new GetProjectionResponse()
                {
                    Success = true, ProjectionVolume = request.FromVolume, Request = request
                };
                response.AddToActivityAsJsonTag("GetProjectionResponse");
                return response;
            }

            var convertMap = await _baseCurrencyConverterService.GetConvertorMapToBaseCurrencyAsync(new GetConvertorMapToBaseCurrencyRequest()
            {
                BrokerId = request.BrokerId,
                BaseAsset = request.ToAsset
            });

            var route = convertMap.Maps.FirstOrDefault(map => map.AssetSymbol == request.FromAsset);

            route.AddToActivityAsJsonTag("route");
            _logger.LogInformation($"Our route : {JsonConvert.SerializeObject(route)}");
            
            if (route == null)
            {
                _logger.LogError($"Route not found. Request: {JsonConvert.SerializeObject(request)}. Map: {JsonConvert.SerializeObject(convertMap)}");
                return new GetProjectionResponse() {Success = false, ErrorText = "Route for projection not found.", Request = request};
            }

            var projectionVolume = request.FromVolume;

            foreach (var operation in route.Operations.OrderBy(operation => operation.Order))
            {
                var price = _currentPricesCache.GetPrice(request.BrokerId, operation.InstrumentPrice);

                if (price == null)
                {
                    _logger.LogError($"Receive NULL GetPrice response for operation {operation}");
                    return new GetProjectionResponse() {Success = false, ErrorText = "Price for map not found.", Request = request};
                }
                if (operation.IsMultiply)
                {
                    projectionVolume *= (operation.UseBid ? price.Bid : price.Ask);
                }
                else
                {
                    projectionVolume /= (operation.UseBid ? price.Bid : price.Ask);
                }
                _logger.LogInformation($"Receive GetPrice response: {JsonConvert.SerializeObject(price)} for operation {operation}");
            }

            response = new GetProjectionResponse() {Success = true, Request = request, ProjectionVolume = projectionVolume};
            
            response.AddToActivityAsJsonTag("GetProjectionResponse");
            
            return response;
        }
    }
}
