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
using NUnit.Framework.Internal;

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
            var status = GameManager.Instance.LocalUser.UserStatus.Value == PlayerStatus.Ready ? PlayerStatus.Lobby : PlayerStatus.Ready;
            GameManager.Instance.SetLocalUserStatus(status);
        };
        SetEnable(false);
    }

    private void OnEnable()
    {
        GameManager.Instance.LocalLobby.onUserJoined += OnPlayerUpdated;
    }

    private void OnDisable()
    {
        GameManager.Instance.LocalLobby.onUserJoined -= OnPlayerUpdated;
    }

    public void SetEnable(bool isActive)
    {
        m_Root.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;

        if (isActive)
            Join();
    }

    private void Join()
    {
        m_Root.Q<TextElement>("Code").text = GameManager.Instance.LocalLobby.LobbyCode.Value;
    }

    private void OnPlayerUpdated(LocalPlayer player)
    {
        m_Root.Q<TextElement>("Players").text = GameManager.Instance.LocalLobby.PlayerCount + " / " + GameManager.Instance.LocalLobby.MaxPlayerCount.Value;
    }
}
