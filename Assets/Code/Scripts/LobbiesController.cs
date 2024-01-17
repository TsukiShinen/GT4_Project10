using UnityEngine;
using UnityEngine.UIElements;

public class LobbiesController : MonoBehaviour
{
	[SerializeField] private UIDocument m_Document;
	[SerializeField] private VisualTreeAsset m_RoomElement;
	
	private VisualElement m_Root;

	private void Start()
	{
		m_Root = m_Document.rootVisualElement;
		m_Root.Q<Button>("Add").clicked += async () =>
		{
			
		};

		m_Root.Q<Button>("Refresh").clicked += async () =>
		{
			
		};

		m_Root.Q<Button>("Join").clicked += async () =>
		{
			
		};
	}

    public void SetEnable(bool isActive)
    {
        m_Root.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
    }
}