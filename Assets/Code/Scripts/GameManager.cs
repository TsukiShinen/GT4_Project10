using UnityEngine;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance { get; private set; }

	private void Awake()
	{
		if (Instance)
		{
			Destroy(this);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public void StartHost()
	{
		// TODO : StartHost
	}
}