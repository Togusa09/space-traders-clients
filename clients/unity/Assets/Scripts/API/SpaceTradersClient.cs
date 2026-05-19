using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using SpaceTraders.Core;

namespace SpaceTraders.API
{
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

        public async Task<T> GetRequest<T>(string endpoint)
        {
            Debug.Log($"[SpaceTradersClient] GET {endpoint}");
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{BaseUrl}{endpoint}"))
            {
                SetHeaders(webRequest);
                var operation = webRequest.SendWebRequest();

                while (!operation.isDone) await Task.Yield();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        Debug.Log($"[SpaceTradersClient] Success: {endpoint} ({webRequest.downloadHandler.text.Length} bytes)");
                        return JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[SpaceTradersClient] Parse Error: {e.Message}\n{webRequest.downloadHandler.text}");
                        throw;
                    }
                }
                else
                {
                    string err = $"Error {webRequest.responseCode}: {webRequest.error}";
                    Debug.LogError($"[SpaceTradersClient] Request failed: {endpoint}\n{err}\n{webRequest.downloadHandler.text}");
                    throw new Exception(err);
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

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[SpaceTradersClient] Success: {endpoint} ({webRequest.downloadHandler.text.Length} bytes)");
                    return webRequest.downloadHandler.text;
                }
                else
                {
                    string err = $"Error {webRequest.responseCode}: {webRequest.error}";
                    Debug.LogError($"[SpaceTradersClient] Failed: {endpoint}\n{err}\n{webRequest.downloadHandler.text}");
                    throw new Exception(err);
                }
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

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[SpaceTradersClient] Success: {endpoint} ({webRequest.downloadHandler.text.Length} bytes)");
                    return JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                }
                else
                {
                    string err = $"Error {webRequest.responseCode}: {webRequest.error}";
                    Debug.LogError($"[SpaceTradersClient] Request failed: {endpoint}\n{err}\n{webRequest.downloadHandler.text}");
                    throw new Exception(err);
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
