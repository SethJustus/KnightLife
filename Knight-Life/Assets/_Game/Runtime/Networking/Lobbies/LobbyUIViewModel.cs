namespace KnightLife.Runtime.Networking.Lobbies
{
    using UnityEngine;
    using UnityEngine.UIElements;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class LobbyUIViewModel : MonoBehaviour
    {
        public List<Lobby> Lobbies { get; private set; }

        private MultiColumnListView lobbyList;

        private void OnEnable()
        {
            // Get the root of the UI
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Find the button by name
            Button button = root.Q<Button>("my-button");
            Button refreshButton = root.Q<Button>("refresh-lobbies");
            this.lobbyList = root.Q<MultiColumnListView>("lobby-list");

            // Add click behavior
            button.clicked += OnCreateLobbyButton_Clicked;
            refreshButton.clicked += GetLobbies;

            Debug.Log("About to run get lobbies");
            //Task.Run(this.GetLobbies);
        }

        public async void GetLobbies()
        {
            Debug.Log("Getting lobbies");
            this.Lobbies = await LobbyManager.Instance.QueryLobbiesAsync();
            lobbyList.itemsSource = Lobbies;
        }

        public void OnCreateLobbyButton_Clicked()
        {
            LobbyManager.Instance.CreateLobbyAsync();           
        }
    }
}