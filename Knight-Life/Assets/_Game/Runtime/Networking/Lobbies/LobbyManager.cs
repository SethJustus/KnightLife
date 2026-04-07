namespace KnightLife.Runtime.Networking.Lobbies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
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

                var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var options = new CreateLobbyOptions 
                {
                    IsPrivate = false,
                    Data = new Dictionary<string, DataObject>
                    {
                        { "JoinCode", new DataObject(DataObject.VisibilityOptions.Public, joinCode) }
                    }
                };

               var lobby = await LobbyService.Instance.CreateLobbyAsync(
                    lobbyName,
                    maxPlayers,
                    options
                );

                //await LobbyService.Instance.JoinLobbyByCodeAsync(lobby.LobbyCode);

                var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

                NetworkManager.Singleton.StartHost();
                NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to create lobby: {e}");
            }
        }

        public async Task JoinLobbyByLobbyObjectAsync(Lobby lobby)
        {
            try
            {
                Debug.Log("Join lobby by object");
                if (lobby == null)
                {
                    Debug.LogError("Lobby is null");
                    return;
                }

                if (!lobby.Data.TryGetValue("JoinCode", out var joinCodeData))
                {
                    Debug.LogError("Lobby does not contain a JoinCode");
                    return;
                }

                Debug.Log("Joining lobby by code");
                await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);

                string joinCode = joinCodeData.Value;
                await JoinLobbyAsync(joinCode);

            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to join lobby: {e}");
            }           
        }

        public async Task JoinLobbyAsync(string joinCode)
        {
            Debug.Log("Trying to join lobby " + joinCode);
            try
            {
                await UnityEngine.Awaitable.MainThreadAsync();
                // Join the relay allocation using the join code
                var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                // Configure Unity Transport with the joined allocation
                var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));

                // Start the client
                NetworkManager.Singleton.StartClient();
                //NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to join lobby: {e}");
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