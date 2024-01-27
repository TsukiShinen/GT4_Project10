using NaughtyAttributes;
using UnityEngine;

namespace ScriptableObjects.GameModes
{
	[CreateAssetMenu(fileName = "GameModeConfig", menuName = "GameMode/Config", order = 0)]
	public class GameModeConfig : ScriptableObject
	{
		[SerializeField] private string m_ModeName;

		[SerializeField] private int m_MaxPlayer;

		[SerializeField] private bool m_HasTeams;

		[SerializeField] private bool m_CanRespawn;

		[SerializeField] [Scene] private string m_SceneName;
		public string ModeName => m_ModeName;
		public int MaxPlayer => m_MaxPlayer;
		public bool HasTeams => m_HasTeams;
		public bool CanRespawn => m_CanRespawn;
		public string SceneName => m_SceneName;
	}
}