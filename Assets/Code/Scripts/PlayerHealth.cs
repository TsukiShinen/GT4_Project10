using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : NetworkBehaviour
{
	[SerializeField] private TMP_Text m_Health;
	[SerializeField] private Slider m_Slider;

	public void InflictDamage(float pDamage, ulong pOwnerId)
	{
		GameManager.Instance.Server_PlayerHit(pDamage, transform, pOwnerId);
	}
}