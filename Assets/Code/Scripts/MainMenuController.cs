using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;

    private void Awake()
    {
        m_Document.rootVisualElement.Q<Button>("Host").clicked += () =>
        {
            NetworkManager.Singleton.StartHost();
        };
        m_Document.rootVisualElement.Q<Button>("Join").clicked += () =>
        {
            NetworkManager.Singleton.StartClient();
        };
    }
}
