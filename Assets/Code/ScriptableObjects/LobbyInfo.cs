using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu(fileName = "LobbyInfo", menuName = "Lobby/Info", order = 0)]
	public class LobbyInfo : ScriptableObject
	{
		[SerializeField] private string m_Name;
		public string Name
		{
			get => m_Name;
			set => m_Name = value;
		}
		
		[SerializeField] private string m_Code;
		public string Code 
		{
			get => m_Code;
			set => m_Code = value;
		}
	}
}