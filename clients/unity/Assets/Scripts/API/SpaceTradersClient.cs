using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

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
                    _instance = FindObjectOfType<SpaceTradersClient>();
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
        }

        public IEnumerator GetRequest<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{BaseUrl}{endpoint}"))
            {
                SetHeaders(webRequest);
                yield return webRequest.SendWebRequest();

                HandleResponse(webRequest, onSuccess, onError);
            }
        }

        public IEnumerator PostRequest<T, R>(string endpoint, R rawData, Action<T> onSuccess, Action<string> onError)
        {
            string jsonData = JsonUtility.ToJson(rawData);
            using (UnityWebRequest webRequest = UnityWebRequest.PostWwwForm($"{BaseUrl}{endpoint}", "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                
                SetHeaders(webRequest);
                yield return webRequest.SendWebRequest();

                HandleResponse(webRequest, onSuccess, onError);
            }
        }

        private void SetHeaders(UnityWebRequest webRequest)
        {
            if (!string.IsNullOrEmpty(_token))
            {
                webRequest.SetRequestHeader("Authorization", $"Bearer {_token}");
            }
            webRequest.SetRequestHeader("Accept", "application/json");
        }

        private void HandleResponse<T>(UnityWebRequest webRequest, Action<T> onSuccess, Action<string> onError)
        {
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    T data = JsonUtility.FromJson<T>(webRequest.downloadHandler.text);
                    onSuccess?.Invoke(data);
                }
                catch (Exception e)
                {
                    onError?.Invoke($"Failed to parse response: {e.Message}");
                }
            }
            else
            {
                string errorMessage = $"Error {webRequest.responseCode}: ";
                if (!string.IsNullOrEmpty(webRequest.downloadHandler.text))
                {
                    errorMessage += webRequest.downloadHandler.text;
                }
                else
                {
                    errorMessage += webRequest.error;
                }
                onError?.Invoke(errorMessage);
            }
        }
    }
}
