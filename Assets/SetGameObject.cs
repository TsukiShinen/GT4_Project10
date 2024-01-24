using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SetGameObject : NetworkBehaviour
{
    [ClientRpc]
    public void SetGameObject_ClientRpc(bool isActive)
    {
        Debug.Log("SetGameObject_ClientRpc");
        gameObject.SetActive(isActive);
    }
}
