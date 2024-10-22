using System.Collections.Generic;
using Network;
using ScriptableObjects.GameModes;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuLobbyController : MonoBehaviour
{
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private CreateLobbyController m_CreateLobby;
	[SerializeField] private VisualTreeAsset m_RoomElement;
	[SerializeField] private GameModes m_GameModes;

	private VisualElement m_Root;

	private void Start()
	{
		m_Root = m_Document.rootVisualElement;

		m_Root.Q<TextField>("Pseudo").value = MultiplayerManager.Instance.PlayerName;
		m_Root.Q<TextField>("Pseudo").RegisterValueChangedCallback(e =>
		{
			MultiplayerManager.Instance.PlayerName = e.newValue;
		});

		m_Root.Q<Button>("Add").clicked += async () => { m_CreateLobby.Open(); };

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
				if (lobby.Data.TryGetValue(LobbyManager.k_KeyGameModeIndex, out var value) &&
				    int.Parse(value.Value) < m_GameModes.GameModeConfigs.Count)
					lobbyView.Q<TextElement>("GameMode").text =
						m_GameModes.GameModeConfigs[int.Parse(value.Value)].ModeName;
				else
					continue;
				lobbyView.Q<TextElement>("PlayerCount").text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
				lobbyView.AddManipulator(new Clickable(e => { LobbyManager.Instance.JoinWithId(lobby.Id); }));
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