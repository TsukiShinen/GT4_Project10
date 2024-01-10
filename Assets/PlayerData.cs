using Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public int PlayerIndex { get; private set; }

    private void Awake()
    {
        PlayerIndex = GameManager.Instance.LocalLobby.PlayerCount - 1;
    }
}
