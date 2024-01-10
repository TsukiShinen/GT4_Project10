using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects.GameModes
{
    [CreateAssetMenu(fileName = "GameModes  ", menuName = "GameMode/Modes", order = 0)]
    public class GameModes : ScriptableObject
    {
        [SerializeField] private List<GameModeConfig> m_GameModesConfig;
        public List<GameModeConfig> GameModeConfigs => m_GameModesConfig;
    }
}