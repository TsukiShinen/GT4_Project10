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
    private ListView m_Team1ListView;

    private List<VisualElement> m_Elements;
    private Dictionary<ulong, VisualElement> m_Dict;

    private void Start()
    {
        m_Root = m_Document.rootVisualElement;
        m_Elements = new List<VisualElement>();
        m_Dict = new Dictionary<ulong, VisualElement>();

        MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += OnPlayerDataNetworkListChanged;

        m_Root.Q<Button>("Quit").clicked += () =>
        {
            LobbyManager.Instance.LeaveLobby();
        };

        m_Root.Q<Button>("Start").clicked += () =>
        {
            if (!IsServer) return;

            var sceneName = MultiplayerManager.Instance.GameModeConfig.SceneName;
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        };

        m_Team1ListView = m_Root.Q<ListView>("PlayerList1");

        for (int i = 0; i < 5; i++)
            AddPlayerElement();

        m_Team1ListView.makeItem = () => new VisualElement();
        m_Team1ListView.bindItem = (element, i) =>
        {
            element.Clear();
            element.Add(m_Elements[i]);
        };
        m_Team1ListView.itemsSource = m_Elements;
        m_Team1ListView.Rebuild();
        
        UpdatePlayers();
    }

    private void OnPlayerDataNetworkListChanged(object sender, EventArgs e)
    {
        UpdatePlayers();
    }

    private void UpdatePlayers()
    {
        for (var i = 0; i < 5; i++)
        {
            Debug.Log(MultiplayerManager.Instance.IsPlayerIndexConnected(i));
            
            var element = m_Elements[i];
            if (MultiplayerManager.Instance.IsPlayerIndexConnected(i))
            {
                element.Q<Button>("Join").style.display = DisplayStyle.None;
                element.Q<TextElement>("Name").text = MultiplayerManager.Instance.GetPlayerDataByIndex(i).PlayerName.ToString();
            }
            else
            {
                element.Q<Button>("Join").style.display = DisplayStyle.Flex;
                element.Q<TextElement>("Name").text = "";
            }
        }
    }

    private void AddPlayerElement()
    {
        var element = m_Player.CloneTree();
        element.Q<Button>("Join").style.display = DisplayStyle.Flex;
        m_Elements.Add(element);
    }
}