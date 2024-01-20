using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
	public ulong ClientId;
	public FixedString64Bytes PlayerName;
	public bool IsTeamOne;
	public FixedString64Bytes PlayerId;

	public bool Equals(PlayerData other)
	{
		return ClientId == other.ClientId && 
		       PlayerName == other.PlayerName && 
		       IsTeamOne == other.IsTeamOne && 
		       PlayerId == other.PlayerId;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref ClientId);
		serializer.SerializeValue(ref PlayerName);
		serializer.SerializeValue(ref IsTeamOne);
		serializer.SerializeValue(ref PlayerId);
	}
}