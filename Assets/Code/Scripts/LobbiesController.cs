using System.Collections.Generic;
using Network;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbiesController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;
    [SerializeField] private VisualTreeAsset m_RoomElement;
    private VisualElement m_Root;

    private void Awake()
    {
        m_Root = m_Document.rootVisualElement;
        m_Root.Q<Button>("Add").clicked += () =>
        {
            ConnectionManager.Instance.CreateLobby();
        };
        
        m_Root.Q<Button>("Refresh").clicked += async () =>
        {
            var lobbies = await ConnectionManager.Instance.ListLobbies();
            var listView = m_Root.Q<ListView>("RoomsList");
            listView.Clear();

            var items = new List<VisualElement>();
            foreach (var lobby in lobbies)
            {
                var lobbyView = m_RoomElement.CloneTree();
                lobbyView.Q<TextElement>("Name").text = lobby.Name;
                lobbyView.Q<TextElement>("PlayerCount").text = $"{lobby.MaxPlayers - lobby.AvailableSlots}/{lobby.MaxPlayers}";
                items.Add(lobbyView);
            }
            
            listView.makeItem = () => new VisualElement();
            listView.bindItem = (element, i) =>
            {
                element.Clear();
                element.Add(items[i]);
            };
            listView.itemsSource = items;
            listView.Rebuild();
        };
    }
}