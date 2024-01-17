using System;
using UnityEngine;
using UnityEngine.UIElements;

public class MessagePopUp : MonoBehaviour
{
	[SerializeField] private UIDocument m_Document;

	private VisualElement m_Root;
	
	public static MessagePopUp Instance { get; private set; }

	private void Awake()
	{
		if (Instance)
		{
			Destroy(this);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);

		m_Root = m_Document.rootVisualElement;
		m_Root.style.display = DisplayStyle.None;
	}

	public void Open(string pTitle, string pMessage, params (string label, Action onClicked)[] pButtons)
	{
		m_Root.Q<TextElement>("Title").text = pTitle;
		m_Root.Q<TextElement>("Message").text = pMessage;
		
		var buttonsElement = m_Root.Q<TextElement>("Message");
		buttonsElement.Clear();
		foreach (var button in pButtons)
		{
			var element = new Button(button.onClicked)
			{
				text = button.label
			};
			buttonsElement.Add(element);
		}

		m_Root.style.display = DisplayStyle.Flex;
	}

	public void Hide()
	{
		m_Root.style.display = DisplayStyle.None;
	}
}