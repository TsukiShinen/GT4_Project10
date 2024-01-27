using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnPlayerSpawn : NetworkBehaviour
{
	[SerializeField] private List<GameObject> m_GameObjectsToDeactivateIfNotOwner;
	[SerializeField] private List<GameObject> m_GameObjectsToDeactivateIfOwner;

	public override void OnNetworkSpawn()
	{
		if (IsOwner)
			foreach (var item in m_GameObjectsToDeactivateIfOwner)
				item.SetActive(false);
		else
			foreach (var item in m_GameObjectsToDeactivateIfNotOwner)
				item.SetActive(false);
	}
}