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
    [SerializeField] private LobbiesController m_LobbiesController;

    private VisualElement m_Root;

    private void Awake()
    {
        m_Root = m_Document.rootVisualElement;
        m_Root.Q<Button>("Ready").clicked += async () =>
        {
            var status = GameManager.Instance.LocalUser.UserStatus.Value == PlayerStatus.Ready ? PlayerStatus.Lobby : PlayerStatus.Ready;
            GameManager.Instance.SetLocalUserStatus(status);
        };

        m_Root.Q<Button>("Code").clicked += async () =>
        {
            GUIUtility.systemCopyBuffer = m_Root.Q<Button>("Code").text;
        };

        m_Root.Q<Button>("Quit").clicked += async () =>
        {
            GameManager.Instance.LeaveLobby();
            SetEnable(false);
            m_LobbiesController.SetEnable(true);
        };

        //m_Root.Q<Button>("Settings").style.display = GameManager.Instance.LocalUser.IsHost.Value ? DisplayStyle.Flex : DisplayStyle.None;
        m_Root.Q<VisualElement>("SettingsPanel").style.display = DisplayStyle.None;
        m_Root.Q<Button>("Settings").clicked += () =>
        {
            m_Root.Q<Button>("SettingsPanel").style.display = DisplayStyle.Flex;
        };
        SetEnable(false);
    }

    private void OnEnable()
    {
        GameManager.Instance.LocalLobby.OnUserJoined += OnPlayerUpdated;
    }

    private void OnDisable()
    {
        GameManager.Instance.LocalLobby.OnUserJoined -= OnPlayerUpdated;
    }

    public void SetEnable(bool isActive)
    {
        m_Root.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;

        if (isActive)
            Join();
    }

    private void Join()
    {
        m_Root.Q<Button>("Code").text = GameManager.Instance.LocalLobby.LobbyCode.Value;
    }

    private void OnPlayerUpdated(LocalPlayer player)
    {
        m_Root.Q<TextElement>("Players").text = GameManager.Instance.LocalLobby.PlayerCount + " / " + GameManager.Instance.LocalLobby.MaxPlayerCount.Value;
    }
}
