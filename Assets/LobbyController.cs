using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class LobbyController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;
    [SerializeField] private VisualTreeAsset m_room;

    private void Awake()
    {
        ListView listView = m_Document.rootVisualElement.Q<ListView>("RoomsList");
        m_Document.rootVisualElement.Q<ScrollView>().verticalScrollerVisibility = ScrollerVisibility.AlwaysVisible;

        var items = new List<VisualElement>();

        for (int i = 0; i < 7; i++)
        {
            items.Add(m_room.CloneTree());
        }

        // Configurez la ListView
        listView.makeItem = () => new VisualElement();
        listView.bindItem = (element, i) =>
        {
            (element as VisualElement).Clear();
            (element as VisualElement).Add(items[i]);
        };
        listView.itemsSource = items;
        listView.Rebuild();
    }
}
