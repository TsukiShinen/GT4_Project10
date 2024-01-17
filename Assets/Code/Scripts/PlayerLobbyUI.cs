using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;
using Network;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;

public class PlayerLobbyUI : MonoBehaviour
{
    [SerializeField] private PlayerData m_PlayerData;
    [SerializeField] private TMP_Text m_NameText;
    [SerializeField] private Image m_IsReadyImage;
    [SerializeField] private Sprite m_ReadySprite;
    [SerializeField] private Sprite m_NotReadySprite;
    [SerializeField] private RectTransform m_LayoutGroup;

    private LocalPlayer m_Player;

    private void Awake()
    {
        m_Player = GameManager.Instance.LocalLobby.GetLocalPlayer(m_PlayerData.PlayerIndex);
        m_NameText.text = m_Player.DisplayName.Value;
    }

    private void OnEnable()
    {
        m_Player.UserStatus.OnChanged += OnLobbyChange;
    }

    private void OnDisable()
    {
        m_Player.UserStatus.OnChanged -= OnLobbyChange;
    }

    private void OnLobbyChange(PlayerStatus statut)
    {
        m_NameText.text = m_Player.DisplayName.Value;
        m_NameText.rectTransform.sizeDelta = new Vector2(m_NameText.preferredWidth, m_NameText.rectTransform.sizeDelta.y);

        m_LayoutGroup.sizeDelta = new Vector2(m_NameText.rectTransform.sizeDelta.x + m_IsReadyImage.rectTransform.sizeDelta.x, m_LayoutGroup.sizeDelta.y);

        if (statut == PlayerStatus.Ready)
        {
            m_IsReadyImage.sprite = m_ReadySprite;
            m_IsReadyImage.color = Color.green;
        }
        else
        {
            m_IsReadyImage.sprite = m_NotReadySprite;
            m_IsReadyImage.color = Color.red;

        }

    }
}
