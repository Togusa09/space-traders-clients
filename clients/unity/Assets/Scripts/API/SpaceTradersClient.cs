using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SpaceTraders.Core;

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

        private static SpaceTradersClient _instance;
        public static SpaceTradersClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SpaceTradersClient>();

                    if (_instance == null)
                    {
                        GameObject go = new GameObject("SpaceTradersClient");
                        _instance = go.AddComponent<SpaceTradersClient>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public static int RateLimitRemaining { get; private set; } = -1;
        public static string RateLimitReset { get; private set; } = string.Empty;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

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

        private void LogResponse(string endpoint, string content, bool isError = false)
        {
            string sanitized = SanitizeResponse(content);
            if (isError)
            {
                Debug.LogError($"[SpaceTradersClient] Failed response for {endpoint}:\n{sanitized}");
            }
            else if (Debug.isDebugBuild)
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
                
                LogResponse(endpoint, rawResponse, isError: true);

                if (code == 401)
                {
                    AuthManager.Instance.HandleTokenUnauthorized();
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

        public async Task<T> GetRequest<T>(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] GET {endpoint}");
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{BaseUrl}{endpoint}"))
            {
                SetHeaders(webRequest);
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                ProcessResponse(webRequest, endpoint);

                try
                {
                    return JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}\n{SanitizeResponse(webRequest.downloadHandler.text)}");
                    throw;
                }
            }
        }

        public async Task<string> GetRequestRaw(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] GET (Raw) {endpoint}");
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{BaseUrl}{endpoint}"))
            {
                SetHeaders(webRequest);
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                ProcessResponse(webRequest, endpoint);
                return webRequest.downloadHandler.text;
            }
        }

        public async Task<T> PostRequest<T, R>(string endpoint, R rawData)
        {
            string jsonData = JsonUtility.ToJson(rawData);
            Debug.Log($"[SpaceTradersClient] POST {endpoint}");
            using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}{endpoint}", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                SetHeaders(webRequest);
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                ProcessResponse(webRequest, endpoint);

                try
                {
                    return JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}\n{SanitizeResponse(webRequest.downloadHandler.text)}");
                    throw;
                }
            }
        }

        public async Task<T> PostRequest<T>(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] POST {endpoint} (Empty)");
            using (UnityWebRequest webRequest = new UnityWebRequest($"{BaseUrl}{endpoint}", "POST"))
            {
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                SetHeaders(webRequest);
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                ProcessResponse(webRequest, endpoint);

                try
                {
                    return JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SpaceTradersClient] Parse Error on {endpoint}: {e.Message}\n{SanitizeResponse(webRequest.downloadHandler.text)}");
                    throw;
                }
            }
        }

        private void SetHeaders(UnityWebRequest webRequest)
        {
            if (string.IsNullOrEmpty(_token))
            {
                _token = AuthManager.Instance.AgentToken;
            }

            if (!string.IsNullOrEmpty(_token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {_token}");
            }
            webRequest.SetRequestHeader("Accept", "application/json");
        }
    }
}

