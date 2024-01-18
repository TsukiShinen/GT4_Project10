using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultNamespace
{
	public class LobbyController : MonoBehaviour
	{
		[SerializeField] private UIDocument m_Document;
	
		private VisualElement m_Root;

		private void Start()
		{
			m_Root = m_Document.rootVisualElement;

			m_Root.Q<Button>("Ready").clicked += LobbyReady.Instance.SetPlayerReady;
		}
	}
}