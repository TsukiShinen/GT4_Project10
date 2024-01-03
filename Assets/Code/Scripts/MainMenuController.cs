using Network;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;

    private void Awake()
    {
        m_Document.rootVisualElement.Q<Button>("Host").clicked += () =>
        {
            ConnectionManager.Instance.CreateRelay();
        };
        m_Document.rootVisualElement.Q<Button>("Join").clicked += () =>
        {
            ConnectionManager.Instance.JoinRelay(m_Document.rootVisualElement.Q<TextField>("RoomCode").text);
        };
    }
}
