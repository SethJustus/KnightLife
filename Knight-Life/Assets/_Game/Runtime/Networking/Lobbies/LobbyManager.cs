namespace KnightLife.Runtime.Networking.Lobbies
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Unity.Netcode;
    using Unity.Netcode.Transports.UTP;
    using Unity.Services.Core;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using Unity.Services.Relay;
    using Unity.Services.Relay.Models;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class LobbyManager : MonoBehaviour
    {
        public Lobby CurrentLobby { get; private set; }

        public static LobbyManager Instance { get; private set; }

        public async void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Debug.LogWarning("Destroying Lobby Manager");
                Destroy(gameObject);
                return;
            }

            await UnityServices.InitializeAsync();
        }

        public async Task CreateLobbyAsync()
        {
            try
            {
                string lobbyName = "My First Lobby";
                int maxPlayers = 4;

                var options = new CreateLobbyOptions 
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "GameMode", new DataObject(DataObject.VisibilityOptions.Public, "Coop") },
                        { "Map", new DataObject(DataObject.VisibilityOptions.Public, "Forest") }
                    }
                };

                CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
                    lobbyName,
                    maxPlayers,
                    options
                );

                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

                NetworkManager.Singleton.StartHost();

                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);

                Debug.Log($"Created lobby: {CurrentLobby.Name}");
                Debug.Log($"Lobby ID: {CurrentLobby.Id}");
                Debug.Log($"Lobby Code: {CurrentLobby.LobbyCode}");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to create lobby: {e}");
            }
        }

        public async Task<List<Lobby>> QueryLobbiesAsync()
        {
            try
            {
                var options = new QueryLobbiesOptions
                {
                    Count = 25,
                    Filters = new List<QueryFilter>
                    {
                        new QueryFilter(
                            QueryFilter.FieldOptions.AvailableSlots,
                            "0",
                            QueryFilter.OpOptions.GT)
                    },
                            Order = new List<QueryOrder>
                    {
                        new QueryOrder(
                            asc: false,
                            field: QueryOrder.FieldOptions.LastUpdated)
                    }
                };

                QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);
                Debug.Log(response.Results.Count);
                return response.Results;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to query lobbies: {ex}");
                return new List<Lobby>();
            }
            
        }
    }
}