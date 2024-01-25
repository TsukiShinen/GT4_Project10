using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlPointGameManager : GameManager
{
    [SerializeField] private ControlPoint m_ControlPoint;
    private int m_PointsToWin = 100;
    private NetworkVariable<float> m_ScoreTeam1;
    private NetworkVariable<float> m_ScoreTeam2;

    protected override void Awake()
    {
        base.Awake();

        m_ScoreTeam1 = new NetworkVariable<float>();
        m_ScoreTeam2 = new NetworkVariable<float>();
    }

    protected void Update()
    {
        if (!NetworkManager.IsServer)
            return;
        Server_UpdateScores();
    }

    protected override void SceneManager_OnLoadEventCompleted(string pSceneName, LoadSceneMode pLoadMode, List<ulong> pClientsCompleted, List<ulong> pClientTimouts)
    {
        foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            var player = m_SpawnManager.SpawnPlayer(clientId);
            m_PlayersGameObjects.Add(clientId, player);
        }
        base.SceneManager_OnLoadEventCompleted(pSceneName, pLoadMode, pClientsCompleted, pClientTimouts);
    }

    private void Server_UpdateScores()
    {
        switch (m_ControlPoint.CurrentState)
        {
            case ControlPointState.Captured:
                int pointsMultiplier = m_ControlPoint.GetPlayersInside().Length;
                Debug.Log("Multiplier : " + pointsMultiplier);
                if (m_ControlPoint.CurrentTeam == 1)
                    m_ScoreTeam1.Value += m_ControlPoint.PointsPerTick * pointsMultiplier;
                else if (m_ControlPoint.CurrentTeam == 2)
                    m_ScoreTeam2.Value += m_ControlPoint.PointsPerTick * pointsMultiplier;

                if (m_ScoreTeam1.Value >= m_PointsToWin || m_ScoreTeam2.Value >= m_PointsToWin)
                {
                    Debug.Log("Team " + (m_ScoreTeam1.Value >= m_PointsToWin ? "1" : "2") + " Win !");
                    m_ControlPoint.CurrentState = ControlPointState.Controlled;
                    EndGame_ClientRpc();
                    break;
                }

                break;
        }
    }

    [ClientRpc]
    private void EndGame_ClientRpc()
    {
        Debug.Log("END");
    }
}
