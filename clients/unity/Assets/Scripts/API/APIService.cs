using System;
using System.Collections;
using SpaceTraders.API.Models;
using UnityEngine;

namespace SpaceTraders.API
{
    public class APIService : MonoBehaviour
    {
        private static APIService _instance;
        public static APIService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<APIService>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("APIService");
                        _instance = go.AddComponent<APIService>();
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

        public void Register(string symbol, string faction, Action<RegistrationResponse> onSuccess, Action<string> onError)
        {
            var data = new RegistrationData { symbol = symbol, faction = faction };
            StartCoroutine(SpaceTradersClient.Instance.PostRequest<RegistrationResponse, RegistrationData>("/register", data, onSuccess, onError));
        }

        public void GetMyAgent(Action<AgentResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SpaceTradersClient.Instance.GetRequest<AgentResponse>("/my/agent", onSuccess, onError));
        }

        public void GetContracts(Action<ContractsResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SpaceTradersClient.Instance.GetRequest<ContractsResponse>("/my/contracts", onSuccess, onError));
        }

        public void GetShips(Action<ShipsResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SpaceTradersClient.Instance.GetRequest<ShipsResponse>("/my/ships", onSuccess, onError));
        }

        public void GetSystems(Action<SystemsResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SpaceTradersClient.Instance.GetRequest<SystemsResponse>("/systems", onSuccess, onError));
        }

        public void GetFactions(Action<FactionsResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SpaceTradersClient.Instance.GetRequest<FactionsResponse>("/factions", onSuccess, onError));
        }
    }
}
