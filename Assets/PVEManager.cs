using System.Collections;
using System.Collections.Generic;
using Unity.FPS.Gameplay;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class PVEManager : GameManager
{
    protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode,
         List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var player = m_SpawnManager.Server_SpawnPlayer(clientId);
            m_PlayersGameObjects.Add(clientId, player);

            PlayerData playerData = MultiplayerManager.Instance.FindPlayerData(clientId);
            if (playerData.PlayerActiveWeaponId != 0)
            {
                Debug.Log("Switch to last weapon");
                player.GetComponent<PlayerWeaponsManager>().SwitchToWeaponIndex(playerData.PlayerActiveWeaponId);
            }
        }

        base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
    }
}
