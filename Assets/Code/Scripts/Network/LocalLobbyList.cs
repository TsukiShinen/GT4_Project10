using System;
using System.Collections.Generic;
using Network;

/// <summary>
/// Used when displaying the lobby list, to indicate when we are awaiting an updated lobby query.
/// </summary>
public enum LobbyQueryState
{
	Empty,
	Fetching,
	Error,
	Fetched
}

/// <summary>
/// Holds data related to the Lobby service itself - The latest retrieved lobby list, the state of retrieval.
/// </summary>
[Serializable]
public class LocalLobbyList
{
	public CallbackValue<LobbyQueryState> QueryState = new CallbackValue<LobbyQueryState>();

	public Action<Dictionary<string, LocalLobby>> OnLobbyListChange;
	private Dictionary<string, LocalLobby> m_CurrentLobbies = new Dictionary<string, LocalLobby>();

	/// <summary>
	/// Maps from a lobby's ID to the local representation of it. This allows us to remember which remote lobbies are which LocalLobbies.
	/// Will only trigger if the dictionary is set wholesale. Changes in the size or contents will not trigger OnChanged.
	/// </summary>
	public Dictionary<string, LocalLobby> CurrentLobbies
	{
		get { return m_CurrentLobbies; }
		set
		{
			m_CurrentLobbies = value;
			OnLobbyListChange?.Invoke(m_CurrentLobbies);
		}
	}

	public void Clear()
	{
		CurrentLobbies = new Dictionary<string, LocalLobby>();
		QueryState.Value = LobbyQueryState.Fetched;
	}
}