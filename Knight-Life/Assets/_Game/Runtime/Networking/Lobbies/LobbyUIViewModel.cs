namespace KnightLife.Runtime.Networking.Lobbies
{
    using NUnit.Framework;
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Unity.Properties;
    using Unity.Services.Lobbies;
    using Unity.Services.Lobbies.Models;
    using UnityEngine;
    using UnityEngine.UIElements;
    using static UnityEditor.Profiling.HierarchyFrameDataView;

    public class LobbyUIViewModel : MonoBehaviour
    {
        [UxmlAttribute, CreateProperty]
        public List<Lobby> Lobbies { get; private set; }

        private MultiColumnListView lobbyList;

        private void OnEnable()
        {
            // Get the root of the UI
            var root = GetComponent<UIDocument>().rootVisualElement;

            // Find the button by name
            Button button = root.Q<Button>("create-lobby-button");
            Button refreshButton = root.Q<Button>("refresh-lobbies-button");
            
            // Add click behavior
            button.clicked += OnCreateLobbyButton_Clicked;
            refreshButton.clicked += GetLobbies;

            this.lobbyList = root.Q<MultiColumnListView>("lobby-list");

            // 1) Set the object that owns the list
            lobbyList.dataSource = this;

            // 2) Bind itemsSource to the list property on that object
            lobbyList.SetBinding("itemsSource", new DataBinding
            {
                dataSourcePath = new PropertyPath(nameof(this.Lobbies))
            });

            // 3) Tell each column how to display a row item
            lobbyList.columns["name"].makeCell = () => new Label();
            lobbyList.columns["name"].bindCell = (element, rowIndex) =>
            {
                var label = (Label)element;
                label.text = Lobbies[rowIndex].Name;
            };

            lobbyList.columns["max-players"].makeCell = () => new Label();
            lobbyList.columns["max-players"].bindCell = (element, rowIndex) =>
            {
                var label = (Label)element;
                label.text = Lobbies[rowIndex].MaxPlayers.ToString();
            };

            lobbyList.columns["current-players"].makeCell = () => new Label();
            lobbyList.columns["current-players"].bindCell = (element, rowIndex) =>
            {
                var label = (Label)element;
                label.text = Lobbies[rowIndex].Players.Count.ToString();
            };
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