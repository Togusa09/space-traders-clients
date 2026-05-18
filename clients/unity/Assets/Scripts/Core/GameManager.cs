using SpaceTraders.API;
using SpaceTraders.Core;
using UnityEngine;

namespace SpaceTraders.Core
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private SpaceTradersClient client;
        [SerializeField] private APIService apiService;
        [SerializeField] private AuthManager authManager;

        private void Start()
        {
            if (authManager.HasAgentToken)
            {
                client.SetToken(authManager.AgentToken);
                Debug.Log("Agent Token loaded. Ready to play.");
                FetchAgentData();
            }
            else
            {
                Debug.Log("No agent token found. Please register or configure in settings.");
            }
        }

        public void OnRegistrationSuccess(string token)
        {
            authManager.SaveAgentToken(token);
            client.SetToken(token);
            FetchAgentData();
        }

        private void FetchAgentData()
        {
            apiService.GetMyAgent(
                response => Debug.Log($"Agent: {response.data.symbol}, Credits: {response.data.credits}"),
                error => Debug.LogError($"Failed to fetch agent: {error}")
            );
        }
    }
}
