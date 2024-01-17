using System.Collections.Generic;
using Network;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuLobbyController : MonoBehaviour
{
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private VisualTreeAsset m_RoomElement;
	
	private VisualElement m_Root;

	private void Start()
	{
		m_Root = m_Document.rootVisualElement;

		m_Root.Q<TextField>("Pseudo").value = GameManager.Instance.PlayerName;
		m_Root.Q<TextField>("Pseudo").RegisterValueChangedCallback(e =>
		{
			GameManager.Instance.PlayerName = e.newValue;
		});
		
		m_Root.Q<Button>("Add").clicked += async () =>
		{
			LobbyManager.Instance.CreateLobby($"{GameManager.Instance.PlayerName}'s Room");
		};

		m_Root.Q<Button>("Refresh").clicked += async () =>
		{
			Debug.Log("Refresh Lobby list");
			LobbyManager.Instance.ListLobbies();
		};

		LobbyManager.Instance.OnLobbyListChanged += (sender, args) =>
		{
			var listView = m_Root.Q<ListView>("RoomsList");
			listView.Clear();

			var items = new List<VisualElement>();
			foreach (var lobby in args.LobbyList)
			{
				var lobbyView = m_RoomElement.CloneTree();
				lobbyView.Q<TextElement>("Name").text = lobby.Name;
				lobbyView.Q<TextElement>("PlayerCount").text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
				lobbyView.AddManipulator(new Clickable(e =>
				{
					LobbyManager.Instance.JoinWithCode(lobby.LobbyCode);
				}));
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

		m_Root.Q<Button>("Join").clicked += async () =>
		{
			LobbyManager.Instance.JoinWithCode(m_Root.Q<TextField>("JoinCode").value);
		};
	}

    public void SetEnable(bool isActive)
    {
        m_Root.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
    }
}