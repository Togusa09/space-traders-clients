using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using SpaceTraders.API;
using SpaceTraders.UI;
using VContainer;

namespace SpaceTraders.Core
{
    public class GameManager : MonoBehaviour
    {
        private AuthManager _authManager;

        [Inject]
        public void Construct(AuthManager authManager)
        {
            _authManager = authManager;
        }

        public void OnRegistrationSuccess(string token)
        {
            _authManager.SaveAgentToken(token);
            SceneManager.LoadScene(SceneNames.MainMenu);
        }
    }
}
