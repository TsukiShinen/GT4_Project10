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

    private Player m_Player;

    private void Start()
    {
        m_Player = ConnectionManager.Instance.GetPlayerById(m_PlayerData.PlayerId);
    }

    private void OnEnable()
    {
        ConnectionManager.Instance.OnPlayerDataChange += OnLobbyChange;     
    }

    private void OnDisable()
    {
        ConnectionManager.Instance.OnPlayerDataChange -= OnLobbyChange;
    }

    private void OnLobbyChange(Dictionary<int, Dictionary<string, ChangedOrRemovedLobbyValue<PlayerDataObject>>> obj)
    {
        m_NameText.text = m_Player.Profile.Name;
        m_NameText.rectTransform.sizeDelta = new Vector2(m_NameText.preferredWidth, m_NameText.rectTransform.sizeDelta.y);

        m_LayoutGroup.sizeDelta = new Vector2(m_NameText.rectTransform.sizeDelta.x + m_IsReadyImage.rectTransform.sizeDelta.x, m_LayoutGroup.sizeDelta.y);

        if (m_Player.Data.TryGetValue("IsReady", out var ready))
        {
            if (ready.Value == true.ToString())
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
}
