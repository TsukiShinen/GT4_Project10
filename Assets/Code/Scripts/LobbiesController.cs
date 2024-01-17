using System.Collections.Generic;
using ScriptableObjects.GameModes;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbiesController : MonoBehaviour
{
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private VisualTreeAsset m_RoomElement;
	[SerializeField] private GameModeConfig m_TempVariableGameMode;
	[SerializeField] private InLobbyController m_InLobbyController;
	private VisualElement m_Root;

	private void Start()
	{
		m_Root = m_Document.rootVisualElement;
		m_Root.Q<Button>("Add").clicked += async () =>
		{
			m_Root.style.display = DisplayStyle.None;
			string pseudo = m_Root.Q<TextField>("Pseudo").value;
			if(pseudo != "")
			{
				GameManager.Instance.SetLocalUserName(pseudo);
			}
            await GameManager.Instance.CreateLobby(false);
			m_InLobbyController.SetEnable(true);
		};

		m_Root.Q<Button>("Refresh").clicked += async () =>
		{
			GameManager.Instance.QueryLobbies();
		};

		GameManager.Instance.LobbyList.OnLobbyListChange += lobbies =>
		{
			var listView = m_Root.Q<ListView>("RoomsList");
			listView.Clear();

			var items = new List<VisualElement>();
			foreach (var lobby in lobbies.Values)
			{
				var lobbyView = m_RoomElement.CloneTree();
				lobbyView.Q<TextElement>("Name").text = lobby.LobbyName.Value;
				lobbyView.Q<TextElement>("PlayerCount").text = $"{lobby.PlayerCount}/{lobby.MaxPlayerCount.Value}";
				lobbyView.AddManipulator(new Clickable(e =>
				{
					GameManager.Instance.JoinLobby(lobby.LobbyID.Value, null);
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
			m_Root.style.display = DisplayStyle.None;
            string pseudo = m_Root.Q<TextField>("Pseudo").value;
            if (pseudo != "")
            {
                GameManager.Instance.SetLocalUserName(pseudo);
            }
            await GameManager.Instance.JoinLobby("", m_Root.Q<TextField>("JoinCode").value);
			m_InLobbyController.SetEnable(true);
		};
	}
}