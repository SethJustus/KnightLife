using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace KnightLife.Runtime.Networking
{
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        public async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("Destroying Network Manager");
                Destroy(gameObject);
                return;
            }

            await UnityServices.InitializeAsync();
            
            await this.SignInAnonymouslyAsync();           
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
