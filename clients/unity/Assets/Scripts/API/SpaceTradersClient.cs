using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
                var parsedError = ParseServerError(e);
                EnrichExceptionData(e, parsedError);

                if (e.ErrorCode == 429)
                {
                    Log.Warning("[SpaceTradersClient] Rate limit hit (429). Headers: {Headers}. Message: {ServerMessage}. RequestId: {RequestId}. Data: {ServerData}. Raw: {RawError}",
                        e.Headers,
                        parsedError.Message,
                        parsedError.RequestId,
                        parsedError.DataJson,
                        parsedError.RawJson);
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

                Log.Error("[SpaceTradersClient] {Info} Failed ({Code}): {Error}. ServerMessage: {ServerMessage}. RequestId: {RequestId}. Data: {ServerData}. Raw: {RawError}",
                    endpointInfo,
                    e.ErrorCode,
                    e.Message,
                    parsedError.Message,
                    parsedError.RequestId,
                    parsedError.DataJson,
                    parsedError.RawJson);
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

        private static void EnrichExceptionData(ApiException e, ParsedServerError parsed)
        {
            e.Data["SpaceTraders.RawErrorJson"] = parsed.RawJson;
            e.Data["SpaceTraders.ServerMessage"] = parsed.Message;
            e.Data["SpaceTraders.RequestId"] = parsed.RequestId;
            e.Data["SpaceTraders.ServerDataJson"] = parsed.DataJson;

            if (parsed.Code.HasValue)
            {
                e.Data["SpaceTraders.ServerErrorCode"] = parsed.Code.Value;
            }
        }

        private static ParsedServerError ParseServerError(ApiException e)
        {
            var raw = e.ErrorContent?.ToString();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return ParsedServerError.Empty;
            }

            if (TryParseErrorJson(raw, out var parsed))
            {
                return parsed;
            }

            // Some logs/handlers can provide escaped JSON as a JSON string literal.
            try
            {
                var unescaped = JsonConvert.DeserializeObject<string>(raw);
                if (!string.IsNullOrWhiteSpace(unescaped) && TryParseErrorJson(unescaped, out parsed))
                {
                    return parsed;
                }
            }
            catch
            {
                // Keep raw payload even if parsing fails.
            }

            return new ParsedServerError
            {
                RawJson = raw,
                Message = string.Empty,
                RequestId = string.Empty,
                DataJson = string.Empty,
                Code = null
            };
        }

        private static bool TryParseErrorJson(string json, out ParsedServerError parsed)
        {
            parsed = ParsedServerError.Empty;

            try
            {
                var root = JToken.Parse(json);
                var errorNode = root["error"] ?? root;

                int? code = null;
                var codeToken = errorNode["code"];
                if (codeToken != null && int.TryParse(codeToken.ToString(), out var parsedCode))
                {
                    code = parsedCode;
                }

                var message = errorNode["message"]?.ToString() ?? string.Empty;
                var requestId = errorNode["requestId"]?.ToString() ?? root["requestId"]?.ToString() ?? string.Empty;

                var dataNode = errorNode["data"] ?? root["data"];
                var dataJson = dataNode != null ? dataNode.ToString(Formatting.None) : string.Empty;

                parsed = new ParsedServerError
                {
                    RawJson = json,
                    Message = message,
                    RequestId = requestId,
                    DataJson = dataJson,
                    Code = code
                };

                return true;
            }
            catch
            {
                return false;
            }
        }

        private sealed class ParsedServerError
        {
            public static ParsedServerError Empty { get; } = new ParsedServerError
            {
                RawJson = string.Empty,
                Message = string.Empty,
                RequestId = string.Empty,
                DataJson = string.Empty,
                Code = null
            };

            public string RawJson { get; set; }
            public string Message { get; set; }
            public string RequestId { get; set; }
            public string DataJson { get; set; }
            public int? Code { get; set; }
        }
    }
}

