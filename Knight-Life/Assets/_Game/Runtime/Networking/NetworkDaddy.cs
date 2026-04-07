using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace KnightLife.Runtime.Networking
{
    public class NetworkDaddy : MonoBehaviour
    {
        public static NetworkDaddy Instance { get; private set; }
        public bool IsReady { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Destroying duplicate NetworkDaddy");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync();
                await SignInAnonymouslyAsync();

                IsReady = true;
                Debug.Log("NetworkDaddy is ready.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize services: {ex}");
            }
        }

        private async Task SignInAnonymouslyAsync()
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            Debug.Log("Signed in!");
            Debug.Log("Player ID: " + AuthenticationService.Instance.PlayerId);
        }
    }
}