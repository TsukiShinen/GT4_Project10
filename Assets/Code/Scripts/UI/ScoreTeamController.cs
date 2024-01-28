using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreTeamController : MonoBehaviour
{
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private VisualTreeAsset m_Row;

	private ListView m_ListViewTeam1;
	private readonly List<VisualElement> m_LstTeam1 = new ();
	private ListView m_ListViewTeam2;
	private readonly List<VisualElement> m_LstTeam2 = new ();
	private void Awake()
	{
		m_ListViewTeam1 = m_Document.rootVisualElement.Q<ListView>("ListTeam1");
		SetupListView(m_ListViewTeam1, m_LstTeam1);
		m_ListViewTeam2 = m_Document.rootVisualElement.Q<ListView>("ListTeam2");
		SetupListView(m_ListViewTeam2, m_LstTeam2);

		MultiplayerManager.Instance.OnPlayerDataNetworkListChanged += (sender, args) => UpdateScore();
		UpdateScore();
	}

	private void Update()
	{
		m_Document.rootVisualElement.style.display = Input.GetKey(KeyCode.Tab) ? DisplayStyle.Flex : DisplayStyle.None;
	}

	private void SetupListView(ListView pListView, List<VisualElement> pListTeam)
	{
		pListView.makeItem = () => new VisualElement();
		pListView.bindItem = (element, i) =>
		{
			element.Clear();
			element.Add(pListTeam[i]);
		};
		pListView.itemsSource = pListTeam;
	}

	private void UpdateScore()
	{
		m_LstTeam1.Clear();
		m_LstTeam2.Clear();
		
		foreach (var playerData in MultiplayerManager.Instance.GetPlayerDatas())
		{
			VisualElement row = m_Row.CloneTree();
			row.Q<Label>("Name").text = playerData.PlayerName.ToString();
			row.Q<Label>("Kills").text = playerData.PlayerKills.ToString();
			row.Q<Label>("Deaths").text = playerData.PlayerDeaths.ToString();
			if (playerData.IsTeamOne)
				m_LstTeam1.Add(row);
			else
				m_LstTeam2.Add(row);
		}
		
		m_ListViewTeam1.Rebuild();
		m_ListViewTeam2.Rebuild();
	}
}