using NaughtyAttributes.Test;
using Unity.Netcode;
using UnityEngine;

public class SetPlayerCamera : NetworkBehaviour
{
	[ClientRpc]
	public void Set_ClientRpc(ClientRpcParams pParam)
	{
		if (Camera.main)
		{
			Camera.main.transform.SetParent(transform, false);

		}
		else
			Debug.LogError("No Camera Main to set");
    }
}