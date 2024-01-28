using System;
using Unity.Collections;
using Unity.Netcode;

public struct PlayerData : IEquatable<PlayerData>, INetworkSerializable
{
	public ulong ClientId;
	public FixedString64Bytes PlayerName;
	public bool IsTeamOne;
	public FixedString64Bytes PlayerId;
	public float PlayerHealth;
	public float PlayerMaxHealth;
	public int PlayerKills;
	public int PlayerDeaths;
    public int PlayerActiveWeaponId;

    public int WeaponAmmo1;
    public int WeaponAmmo1Max;
    public int WeaponAmmo2;
    public int WeaponAmmo2Max;
    public int WeaponAmmo3;
    public int WeaponAmmo3Max;

    public bool IsAlive => PlayerHealth > 0;

	public bool Equals(PlayerData other)
	{
		return ClientId == other.ClientId &&
		       PlayerName == other.PlayerName &&
		       IsTeamOne == other.IsTeamOne &&
		       PlayerId == other.PlayerId &&
		       PlayerHealth == other.PlayerHealth &&
		       PlayerMaxHealth == other.PlayerMaxHealth &&
		       PlayerKills == other.PlayerKills &&
		       PlayerDeaths == other.PlayerDeaths &&
               PlayerActiveWeaponId == other.PlayerActiveWeaponId; ;
	}

	public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
	{
		serializer.SerializeValue(ref ClientId);
		serializer.SerializeValue(ref PlayerName);
		serializer.SerializeValue(ref IsTeamOne);
		serializer.SerializeValue(ref PlayerId);
		serializer.SerializeValue(ref PlayerHealth);
		serializer.SerializeValue(ref PlayerMaxHealth);
		serializer.SerializeValue(ref PlayerKills);
		serializer.SerializeValue(ref PlayerDeaths);
		serializer.SerializeValue(ref PlayerActiveWeaponId);
	}
}