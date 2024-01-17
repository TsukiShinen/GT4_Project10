using UnityEngine;

namespace ScriptableObjects.GameModes
{
	[CreateAssetMenu(fileName = "GameModeConfig", menuName = "GameMode/Config", order = 0)]
	public class GameModeConfig : ScriptableObject
	{
		[SerializeField] private string m_Name;

		[SerializeField] private int m_MaxPlayer;
		public string Name => m_Name;
		public int MaxPlayer => m_MaxPlayer;
	}
}