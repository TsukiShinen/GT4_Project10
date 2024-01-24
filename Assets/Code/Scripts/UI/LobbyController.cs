using System;
using System.Collections.Generic;
using System.Linq;
using Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class LobbyController : NetworkBehaviour
{
    [SerializeField] private UIDocument m_Document;
    [SerializeField] private VisualTreeAsset m_Player;

    private VisualElement m_Root;
    private List<VisualElement> m_Elements1;
    private List<VisualElement> m_Elements2;

    private void Awake()
    {
        MessagePopUp.Instance.Hide();
    }

    private void Start()
    {
        m_Root = m_Document.rootVisualElement;

        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;

        m_Root.Q<Button>("Quit").clicked += () =>
        {
            LobbyManager.Instance.LeaveLobby();
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
            SceneManager.LoadScene("Base", LoadSceneMode.Single);
        };

        if (IsServer)
        {
            m_Root.Q<Button>("Start").clicked += () =>
            {
                if (!IsServer) return;
                var sceneName = MultiplayerManager.Instance.GameModeConfig.SceneName;
                LobbyManager.Instance.DeleteLobby();
                NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            };
        }
        else
        {
            m_Root.Q<Button>("Start").style.display = DisplayStyle.None;
        }


        InitPlayerList();
        UpdatePlayers();
    }

    private void InitPlayerList()
    {
        if (MultiplayerManager.Instance.GameModeConfig.HasTeams)
            InitPlayerListWithTeam();
        else
            InitPlayerListWithoutTeam();
    }

    private void InitPlayerListWithTeam()
    {
        m_Elements1 = SetupListElement(MultiplayerManager.Instance.GameModeConfig.MaxPlayer, true);
        m_Elements2 = SetupListElement(MultiplayerManager.Instance.GameModeConfig.MaxPlayer, false);
        
        _ = SetupList("PlayerList1", m_Elements1);
        _ = SetupList("PlayerList2", m_Elements2);
    }

    private void InitPlayerListWithoutTeam()
    {
        m_Elements1 = SetupListElement(MultiplayerManager.Instance.GameModeConfig.MaxPlayer, true);
        
        _ = SetupList("PlayerList1", m_Elements1);
        m_Root.Q("Team1").Q<TextElement>("TeamName").text = "Players";
        m_Root.Q("Team2").style.display = DisplayStyle.None;
    }

    private ListView SetupList(string pName, List<VisualElement> pRefList)
    {
        var view = m_Root.Q<ListView>(pName);
        view.makeItem = () => new VisualElement();
        view.bindItem = (element, i) =>
        {
            element.Clear();
            element.Add(pRefList[i]);
        };
        view.itemsSource = pRefList;
        view.Rebuild();
        return view;
    }

    private void ClearPlayerList(List<VisualElement> pList)
    {
        foreach (var item in pList)
        {
            item.Q<Button>("Join").style.display = MultiplayerManager.Instance.GameModeConfig.HasTeams ? DisplayStyle.Flex : DisplayStyle.None;
            item.Q<TextElement>("Name").text = "";
        }
    }

    private List<VisualElement> SetupListElement(int pNbrElements, bool pIsTeamOne)
    {
        var list = new List<VisualElement>();
        for (var i = 0; i < MultiplayerManager.Instance.GameModeConfig.MaxPlayer; i++)
        {
            var element = m_Player.CloneTree();
            element.Q<Button>("Join").clicked += () =>
            {
                MultiplayerManager.Instance.SetPlayerTeamServerRpc(pIsTeamOne);
            };
            list.Add(element);
        }

        return list;
    }

    private void OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        UpdatePlayers();
    }

    private void UpdatePlayers()
    {
        if (MultiplayerManager.Instance.GameModeConfig.HasTeams)
            UpdatePlayersWithTeam();
        else
            UpdatePlayersWithoutTeam();
    }

    private void UpdatePlayersWithTeam()
    {
        ClearPlayerList(m_Elements1);
        ClearPlayerList(m_Elements2);
        
        for (var i = 0; i < MultiplayerManager.Instance.MaxPlayerAmount; i++)
        {
            if (!MultiplayerManager.Instance.IsPlayerIndexConnected(i)) continue;

            var player = MultiplayerManager.Instance.GetPlayerDataByIndex(i);
            var list = player.IsTeamOne ? m_Elements1 : m_Elements2;
            var element = list.First(e => e.Q<Button>("Join").style.display == DisplayStyle.Flex);
            
            element.Q<Button>("Join").style.display = DisplayStyle.None;
            element.Q<TextElement>("Name").text = player.PlayerName.ToString();
        }
    }

    private void UpdatePlayersWithoutTeam()
    {
        ClearPlayerList(m_Elements1);
        
        for (var i = 0; i < MultiplayerManager.Instance.MaxPlayerAmount; i++)
        {
            Debug.Log(MultiplayerManager.Instance.IsPlayerIndexConnected(i));

            if (!MultiplayerManager.Instance.IsPlayerIndexConnected(i)) continue;

            var player = MultiplayerManager.Instance.GetPlayerDataByIndex(i);
            
            m_Elements1[i].Q<Button>("Join").style.display = DisplayStyle.None;
            m_Elements1[i].Q<TextElement>("Name").text = player.PlayerName.ToString();
        }
    }
}