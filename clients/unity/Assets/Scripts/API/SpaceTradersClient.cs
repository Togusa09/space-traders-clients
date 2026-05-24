using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpaceTraders.Core;
using SpaceTraders.Generated.Api;
using SpaceTraders.Generated.Client;
using VContainer;
using Unity.Logging;

namespace SpaceTraders.API
{
    public class SpaceTradersClient : MonoBehaviour
    {
        private AuthManager _authManager;
        private Configuration _configuration;
        private ApiClient _apiClient;
        private RateLimiter _rateLimiter;

        // Generated APIs
        public AgentsApi Agents { get; private set; }
        public ContractsApi Contracts { get; private set; }
        public FactionsApi Factions { get; private set; }
        public FleetApi Fleet { get; private set; }
        public SystemsApi Systems { get; private set; }
        public GlobalApi Global { get; private set; }

        [Inject]
        public void Construct(AuthManager authManager)
        {
            _authManager = authManager;
            _rateLimiter = new RateLimiter(2, 10); // 2 rps, 10 burst
            InitializeClient();
        }

        private void InitializeClient()
        {
            _configuration = new Configuration
            {
                BasePath = "https://api.spacetraders.io/v2"
            };

            // Set initial token if available
            UpdateToken();

            _apiClient = new ApiClient(_configuration.BasePath);
            
            // Initialize generated APIs
            Agents = new AgentsApi(_apiClient, _apiClient, _configuration);
            Contracts = new ContractsApi(_apiClient, _apiClient, _configuration);
            Factions = new FactionsApi(_apiClient, _apiClient, _configuration);
            Fleet = new FleetApi(_apiClient, _apiClient, _configuration);
            Systems = new SystemsApi(_apiClient, _apiClient, _configuration);
            Global = new GlobalApi(_apiClient, _apiClient, _configuration);

            Log.Info("[SpaceTradersClient] Generated API client initialized.");
        }

        public void SetToken(string token)
        {
            _configuration.AccessToken = token;
            Log.Info("[SpaceTradersClient] Token updated in configuration.");
        }

        private void UpdateToken()
        {
            if (_authManager != null && _authManager.HasAgentToken)
            {
                SetToken(_authManager.AgentToken);
            }
        }

        /// <summary>
        /// Wrapper to handle common API response logic, retries, and errors.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<ApiResponse<T>>> apiCall, string endpointInfo = "")
        {
            try
            {
                await _rateLimiter.WaitAsync();
                var response = await apiCall();
                Log.Info("[SpaceTradersClient] {Info} Success ({Code})", endpointInfo, response.StatusCode);
                return response.Data;
            }
            catch (ApiException e)
            {
                if (e.ErrorCode == 429)
                {
                    Log.Warning("[SpaceTradersClient] Rate limit hit (429). Headers: {Headers}", e.Headers);
                    // Extract x-ratelimit-reset if possible
                    if (e.Headers != null && e.Headers.TryGetValue("x-ratelimit-reset", out var resetHeader))
                    {
                        var firstValue = resetHeader.FirstOrDefault();
                        if (!string.IsNullOrEmpty(firstValue) && DateTime.TryParse(firstValue, out var resetTime))
                        {
                            _rateLimiter.SetResetTime(resetTime);
                        }
                    }
                    
                    // Simple retry once after 429
                    await _rateLimiter.WaitAsync();
                    var retryResponse = await apiCall();
                    return retryResponse.Data;
                }

                Log.Error("[SpaceTradersClient] {Info} Failed ({Code}): {Error}", endpointInfo, e.ErrorCode, e.Message);
                if (e.ErrorCode == 401)
                {
                    _authManager.HandleTokenUnauthorized();
                }
                throw;
            }
            catch (Exception e)
            {
                Log.Error("[SpaceTradersClient] {Info} Unexpected error: {Error}", endpointInfo, e.Message);
                throw;
            }
        }
    }
}

