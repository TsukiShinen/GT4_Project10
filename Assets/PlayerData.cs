using Network;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public string PlayerId { get; private set; }

    private void Awake()
    {
        PlayerId = ConnectionManager.Instance.Lobby.Players[ConnectionManager.Instance.Lobby.Players.Count - 1].Id;
    }
}
