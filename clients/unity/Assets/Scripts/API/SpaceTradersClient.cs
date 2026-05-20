using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SpaceTraders.Core;
using Newtonsoft.Json;
using VContainer;

namespace SpaceTraders.API
{
    public class SpaceTradersApiException : Exception
    {
        public long ResponseCode { get; }
        public string ResponseBody { get; }

        public SpaceTradersApiException(long responseCode, string message, string responseBody) 
            : base(message)
        {
            ResponseCode = responseCode;
            ResponseBody = responseBody;
        }
    }

    public class SpaceTradersUnauthorizedException : SpaceTradersApiException
    {
        public SpaceTradersUnauthorizedException(string message, string responseBody) 
            : base(401, message, responseBody) { }
    }

    public class SpaceTradersRateLimitException : SpaceTradersApiException
    {
        public SpaceTradersRateLimitException(string message, string responseBody) 
            : base(429, message, responseBody) { }
    }

    public class SpaceTradersClient : MonoBehaviour
    {
        private const string BaseUrl = "https://api.spacetraders.io/v2";
        private string _token;

        private AuthManager _authManager;

        [Inject]
        public void Construct(AuthManager authManager)
        {
            _authManager = authManager;
        }

        public static int RateLimitRemaining { get; private set; } = -1;
        public static string RateLimitReset { get; private set; } = string.Empty;

        public void SetToken(string token)
        {
            _token = token;
            Debug.Log("[SpaceTradersClient] Token updated.");
        }

        private string SanitizeResponse(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            // Redact bearer tokens or general "token":"..." patterns
            string sanitized = System.Text.RegularExpressions.Regex.Replace(
                text,
                "\"token\"\\s*:\\s*\"[^\"]+\"",
                "\"token\":\"[REDACTED]\""
            );
            
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                "Bearer\\s+[A-Za-z0-9-_=]+\\.[A-Za-z0-9-_=]+\\.[A-Za-z0-9-_.+/=]+",
                "Bearer [REDACTED]"
            );
            
            sanitized = System.Text.RegularExpressions.Regex.Replace(
                sanitized,
                "Authorization:\\s*Bearer\\s+[^\\s]+",
                "Authorization: Bearer [REDACTED]"
            );

            return sanitized;
        }

        private void LogResponse(string endpoint, string content, bool isError = false, long responseCode = 0)
        {
            string sanitized = SanitizeResponse(content);
            if (isError)
            {
                if (Debug.isDebugBuild || Application.isEditor)
                {
                    Debug.LogError($"[SpaceTradersClient] Failed response for {endpoint} (Code: {responseCode}):\n{sanitized}");
                }
                else
                {
                    Debug.LogError($"[SpaceTradersClient] Failed response for {endpoint} (Code: {responseCode})");
                }
            }
            else if (Debug.isDebugBuild || Application.isEditor)
            {
                Debug.Log($"[SpaceTradersClient] Success response for {endpoint} ({content.Length} bytes):\n{sanitized}");
            }
            else
            {
                Debug.Log($"[SpaceTradersClient] Success response for {endpoint} ({content.Length} bytes)");
            }
        }

        private void ExtractRateLimitHeaders(UnityWebRequest webRequest)
        {
            string remainingStr = webRequest.GetResponseHeader("x-rate-limit-remaining");
            if (int.TryParse(remainingStr, out int remaining))
            {
                RateLimitRemaining = remaining;
            }
            
            string resetStr = webRequest.GetResponseHeader("x-rate-limit-reset");
            if (!string.IsNullOrEmpty(resetStr))
            {
                RateLimitReset = resetStr;
            }
        }

        private void ProcessResponse(UnityWebRequest webRequest, string endpoint)
        {
            ExtractRateLimitHeaders(webRequest);

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                long code = webRequest.responseCode;
                string rawResponse = webRequest.downloadHandler?.text ?? string.Empty;
                string errMessage = $"Error {code}: {webRequest.error}";
                
                LogResponse(endpoint, rawResponse, isError: true, responseCode: code);

                if (code == 401)
                {
                    _authManager.HandleTokenUnauthorized();
                    throw new SpaceTradersUnauthorizedException(errMessage, rawResponse);
                }
                else if (code == 429)
                {
                    throw new SpaceTradersRateLimitException(errMessage, rawResponse);
                }
                else
                {
                    throw new SpaceTradersApiException(code, errMessage, rawResponse);
                }
            }
            else
            {
                string rawResponse = webRequest.downloadHandler?.text ?? string.Empty;
                LogResponse(endpoint, rawResponse, isError: false);
            }
        }

        private async Task<string> ExecuteWithRetry(Func<UnityWebRequest> requestFactory, string endpoint, int maxRetries = 3)
        {
            int attempts = 0;
            int delayMs = 1000;

            while (true)
            {
                attempts++;
                UnityWebRequest webRequest = requestFactory();
                try
                {
                    SetHeaders(webRequest);
                    var operation = webRequest.SendWebRequest();

                    while (!operation.isDone) await Task.Yield();

                    ProcessResponse(webRequest, endpoint);
                    string text = webRequest.downloadHandler.text;
                    webRequest.Dispose();
                    return text;
                }
                catch (SpaceTradersRateLimitException)
                {
                    webRequest.Dispose();
                    if (attempts >= maxRetries)
                    {
                        throw;
                    }

                    int backoffMs = delayMs;
                    if (!string.IsNullOrEmpty(RateLimitReset))
                    {
                        if (DateTime.TryParse(RateLimitReset, out DateTime resetTime))
                        {
                            double seconds = (resetTime - DateTime.UtcNow).TotalSeconds;
                            if (seconds > 0 && seconds < 10)
                            {
                                backoffMs = Mathf.CeilToInt((float)seconds * 1000) + 100;
                            }
                        }
                    }

                    int jitter = UnityEngine.Random.Range(100, 300);
                    backoffMs += jitter;

                    Debug.LogWarning($"[SpaceTradersClient] Rate limit (429) hit on {endpoint}. Retrying attempt {attempts}/{maxRetries} after {backoffMs}ms...");
                    await Task.Delay(backoffMs);
                    delayMs *= 2;
                }
                catch (SpaceTradersApiException ex) when (ex.ResponseCode == 0 || ex.ResponseCode >= 500 || ex.ResponseCode == 408)
                {
                    webRequest.Dispose();
                    if (attempts >= maxRetries)
                    {
                        throw;
                    }

                    int backoffMs = delayMs + UnityEngine.Random.Range(100, 300);
                    Debug.LogWarning($"[SpaceTradersClient] Transient error (Code: {ex.ResponseCode}) on {endpoint}. Retrying attempt {attempts}/{maxRetries} after {backoffMs}ms...");
                    await Task.Delay(backoffMs);
                    delayMs *= 2;
                }
                catch (Exception)
                {
                    webRequest.Dispose();
                    throw;
                }
            }
        }

        public async Task<T> GetRequest<T>(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] GET {endpoint}");
            string responseText = await ExecuteWithRetry(() => UnityWebRequest.Get($"{BaseUrl}{endpoint}"), endpoint);
            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception e)
            {
                if (Debug.isDebugBuild || Application.isEditor)
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}\n{SanitizeResponse(responseText)}");
                }
                else
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}");
                }
                throw;
            }
        }

        public async Task<string> GetRequestRaw(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] GET (Raw) {endpoint}");
            return await ExecuteWithRetry(() => UnityWebRequest.Get($"{BaseUrl}{endpoint}"), endpoint);
        }

        public async Task<T> PostRequest<T, R>(string endpoint, R rawData)
        {
            string jsonData = JsonConvert.SerializeObject(rawData);
            Debug.Log($"[SpaceTradersClient] POST {endpoint}");
            string responseText = await ExecuteWithRetry(() => {
                var webRequest = new UnityWebRequest($"{BaseUrl}{endpoint}", "POST");
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                return webRequest;
            }, endpoint);

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception e)
            {
                if (Debug.isDebugBuild || Application.isEditor)
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}\n{SanitizeResponse(responseText)}");
                }
                else
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}");
                }
                throw;
            }
        }

        public async Task<T> PostRequest<T>(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] POST {endpoint} (Empty)");
            string responseText = await ExecuteWithRetry(() => {
                var webRequest = new UnityWebRequest($"{BaseUrl}{endpoint}", "POST");
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                return webRequest;
            }, endpoint);

            try
            {
                return JsonConvert.DeserializeObject<T>(responseText);
            }
            catch (Exception e)
            {
                if (Debug.isDebugBuild || Application.isEditor)
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}\n{SanitizeResponse(responseText)}");
                }
                else
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}");
                }
                throw;
            }
        }

        private void SetHeaders(UnityWebRequest webRequest)
        {
            if (string.IsNullOrEmpty(_token))
            {
                _token = _authManager.AgentToken;
            }

            if (!string.IsNullOrEmpty(_token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {_token}");
            }
            webRequest.SetRequestHeader("Accept", "application/json");
        }
    }
}
