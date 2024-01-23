using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private TMP_Text m_Health;
    [SerializeField] private Slider m_Slider;

    private void Start()
    {
        UpdateUI();

        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;
    }

    public void InflictDamage(float pDamage, ulong pOwnerId)
    {
        MultiplayerManager.Instance.PlayerHit(pDamage, gameObject, pOwnerId);
    }

    private void OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        var index = MultiplayerManager.Instance.FindPlayerDataIndex(OwnerClientId);
        var playerData = MultiplayerManager.Instance.GetPlayerDataByIndex(index);
        m_Health.text = playerData.PlayerHealth.ToString();
        m_Slider.value = playerData.PlayerHealth / playerData.PlayerMaxHealth;
    }

    [ClientRpc]
    public void RespawnPlayerClientRpc(Vector3 position)
    {
        transform.position = position;
    }
}
