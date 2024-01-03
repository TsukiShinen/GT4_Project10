using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument m_Document;

    private void Awake()
    {
        m_Document.rootVisualElement.Q<Button>("Host").clicked += () =>
        {
            TestLobby.Instance.CreateLobby();
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetComponent<UnityTransport>().SetConnectionData(
                m_Document.rootVisualElement.Q<TextField>("Address").value, 
                ushort.Parse(m_Document.rootVisualElement.Q<TextField>("Port").value)
            );
            NetworkManager.Singleton.StartHost();
        };
        m_Document.rootVisualElement.Q<Button>("Join").clicked += () =>
        {
            TestLobby.Instance.ListLobbies();
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetComponent<UnityTransport>().SetConnectionData(
                m_Document.rootVisualElement.Q<TextField>("Address").value,
                ushort.Parse(m_Document.rootVisualElement.Q<TextField>("Port").value)
            );
            NetworkManager.Singleton.StartClient();
        };
    }
}
