using Unity.Netcode;
using UnityEngine;

public class SetPlayerCamera : NetworkBehaviour
{
	[ClientRpc]
	public void Set_ClientRpc(ClientRpcParams pParam)
	{
		Debug.Log("Set_ClientRpc");
		if (Camera.main)
			Camera.main.transform.SetParent(transform);
		else
			Debug.LogError("No Camera Main to set");
	}
}