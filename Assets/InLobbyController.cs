using Network;
using Unity.Services.Lobbies.Models;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class InLobbyController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;

    private VisualElement m_Root;
    private Lobby m_Lobby;

    private void Awake()
    {
        m_Root = m_Document.rootVisualElement;
        SetEnable(false);
    }

    private void OnEnable()
    {
        ConnectionManager.Instance.OnLobbyChangeAction += OnPlayerUpdated;
    }

    private void OnDisable()
    {
        ConnectionManager.Instance.OnLobbyChangeAction -= OnPlayerUpdated;
    }

    public void SetEnable(bool isActive)
    {
        m_Root.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;

        if (isActive)
            Join();
    }

    private void Join()
    {
        m_Lobby = ConnectionManager.Instance.Lobby;
        m_Root.Q<TextElement>("Code").text = m_Lobby.LobbyCode;
    }

    private void OnPlayerUpdated()
    {
        m_Root.Q<TextElement>("Players").text = m_Lobby.Players.Count + " / " + m_Lobby.MaxPlayers;
        
    }
}
