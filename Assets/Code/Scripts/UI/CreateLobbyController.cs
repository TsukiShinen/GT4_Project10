using System.Linq;
using Network;
using ScriptableObjects.GameModes;
using UnityEngine;
using UnityEngine.UIElements;


public class CreateLobbyController : MonoBehaviour
{
	
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private GameModes m_GameModes;
	
	public VisualElement Root;
	
	private TextField m_Name;
	private Toggle m_IsPrivate;
	private DropdownField m_GameMode;

	private void Awake()
	{
		Root = m_Document.rootVisualElement;
		Root.style.display = DisplayStyle.None;

		Root.Q<Button>("Quit").clicked += () =>
		{
			Root.style.display = DisplayStyle.None;
		};
		
		m_Name = Root.Q<TextField>("RoomName");
		
		m_IsPrivate = Root.Q<Toggle>("IsPrivate");
		
		m_GameMode = Root.Q<DropdownField>("GameMode");
		m_GameMode.choices = m_GameModes.GameModeConfigs.Select(g => g.ModeName).ToList();
		m_GameMode.value = m_GameModes.GameModeConfigs[0].ModeName;

		Root.Q<Button>("Create").clicked += () =>
		{
			LobbyManager.Instance.CreateLobby(m_Name.value, m_GameModes.GameModeConfigs.Find(g => g.ModeName == m_GameMode.value) , m_IsPrivate.value);
		};
	}

	public void Open()
	{
		Root.style.display = DisplayStyle.Flex;

		m_Name.value = $"{MultiplayerManager.Instance.PlayerName}'s Room";
	}
}