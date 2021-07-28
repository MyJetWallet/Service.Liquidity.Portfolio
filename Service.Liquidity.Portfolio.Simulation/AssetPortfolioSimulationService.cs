using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Service.Liquidity.Portfolio.Grpc.Simulation;
using Service.Liquidity.Portfolio.Grpc.Simulation.Models;

namespace Service.Liquidity.Portfolio.Simulation
{
    public class AssetPortfolioSimulationService : IAssetPortfolioSimulationService
    {
        private readonly AssetPortfolioSimulationStorage _assetPortfolioSimulationStorage;

        public AssetPortfolioSimulationService(AssetPortfolioSimulationStorage assetPortfolioSimulationStorage)
        {
            _assetPortfolioSimulationStorage = assetPortfolioSimulationStorage;
        }

        public async Task<CreateNewSimulationResponse> CreateNewSimulation()
        {
            PortfolioSimulation newSimulation;
            try
            {
                newSimulation = await _assetPortfolioSimulationStorage.CreateNewSimulation();
            }
            catch (Exception ex)
            {
                return new CreateNewSimulationResponse() {Success = false, ErrorText = ex.Message};
            }
            return new CreateNewSimulationResponse() {Success = true, Portfolio = newSimulation};
        }

        public async Task<GetSimulationListResponse> GetSimulationList()
        {
            List<PortfolioSimulation> simulationList;
            try
            {
                simulationList = await _assetPortfolioSimulationStorage.GetSimulationList();
            }
            catch (Exception ex)
            {
                return new GetSimulationListResponse() {Success = false, ErrorText = ex.Message};
            }
            return new GetSimulationListResponse() {Success = true, SimulationList = simulationList};
        }

        public async Task<GetSimulationResponse> GetSimulation(GetSimulationRequest request)
        {
            PortfolioSimulation simulation;
            try
            {
                simulation = await _assetPortfolioSimulationStorage.GetSimulation(request.SimulationId);
            }
            catch (Exception ex)
            {
                return new GetSimulationResponse() {Success = false, ErrorText = ex.Message};
            }
            if (request.SimulationId == 0 || simulation == null)
            {
                return new GetSimulationResponse() {Success = false, ErrorText = "Not found"};
            }
            return new GetSimulationResponse() {Success = true, Portfolio = simulation};
        }

        public async Task<ReportSimulationTradeResponse> ReportSimulationTrade(ReportSimulationTradeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.BaseAsset) ||
                string.IsNullOrWhiteSpace(request.QuoteAsset) ||
                request.BaseVolume == 0 ||
                request.QuoteVolume == 0 ||
                request.BaseAssetIndexPrice == 0 ||
                request.QuoteAssetIndexPrice == 0 ||
                request.SimulationId == 0)
            {
                return new ReportSimulationTradeResponse() {Success = false, ErrorText = "Bad request"};
            }
            try
            {
                await _assetPortfolioSimulationStorage.ReportSimulationTrade(request);
            }
            catch (Exception ex)
            {
                return new ReportSimulationTradeResponse() {Success = false, ErrorText = ex.Message};
            }
            return new ReportSimulationTradeResponse() {Success = true};
        }
    }
}
