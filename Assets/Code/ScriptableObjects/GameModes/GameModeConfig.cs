using UnityEngine;

namespace ScriptableObjects.GameModes
{
    [CreateAssetMenu(fileName = "GameModeConfig", menuName = "GameMode/Config", order = 0)]
    public class GameModeConfig : ScriptableObject
    {
        [SerializeField] private string m_Name;
        public string Name => m_Name;
        
        [SerializeField] private int m_MaxPlayer;
        public int MaxPlayer => m_MaxPlayer;
    }
}