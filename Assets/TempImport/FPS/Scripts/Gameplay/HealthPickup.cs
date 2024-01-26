using System;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class HealthPickup : Pickup
    {
        [Header("Parameters")] [Tooltip("Amount of health to heal on pickup")]
        public float HealAmount;

        protected override void OnPicked(PlayerData playerData)
        {
            playerData.PlayerHealth = Mathf.Clamp(playerData.PlayerHealth + HealAmount, 0, 100);
            int index = MultiplayerManager.Instance.FindPlayerDataIndex(playerData.ClientId);
            MultiplayerManager.Instance.GetPlayerDatas()[index] = playerData;
            PlayPickupFeedback();
            Destroy(gameObject);
        }
    }
}