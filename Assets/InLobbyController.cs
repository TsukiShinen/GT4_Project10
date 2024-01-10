using Network;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;
using System;

public class InLobbyController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;

    private VisualElement m_Root;
    private Lobby m_Lobby;

    private void Awake()
    {
        m_Root = m_Document.rootVisualElement;
        m_Root.Q<Button>("Ready").clicked += async () =>
        {
            var player = ConnectionManager.Instance.GetOwnPlayer();
            UpdatePlayerOptions options = new UpdatePlayerOptions();
            if(player.Data.TryGetValue("IsReady", out var value))
            {
                value.Value = (!bool.Parse(value.Value)).ToString();
                options.Data = player.Data;
                await LobbyService.Instance.UpdatePlayerAsync(ConnectionManager.Instance.Lobby.Id, player.Id, options);
            }
        };
        SetEnable(false);
    }

    private void OnEnable()
    {
        ConnectionManager.Instance.OnPlayerJoin += OnPlayerUpdated;
    }

    private void OnDisable()
    {
        ConnectionManager.Instance.OnPlayerJoin -= OnPlayerUpdated;
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

    private void OnPlayerUpdated(List<LobbyPlayerJoined> obj)
    {
        m_Root.Q<TextElement>("Players").text = obj.Count + " / " + m_Lobby.MaxPlayers;
    }
}
