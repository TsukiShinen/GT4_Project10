using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private const string k_KeyRelay = "Relay";
    public static async Task CreateRelay(Lobby pLobby)
    {
        try
        {
            var allocation = await RelayService.Instance.CreateAllocationAsync(8);
            var relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Create Relay : {relayCode}");

            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartHost();

            if (pLobby != null)
            {
                await Lobbies.Instance.UpdateLobbyAsync(pLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                        {
                            { k_KeyRelay, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                        }
                });
            }
            GameManager.Instance.HostSetRelayCode(relayCode);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public static async Task JoinRelay(string pCode)
    {
        try
        {
            Debug.Log("Join : " + pCode + "!");
            var allocation = await RelayService.Instance.JoinAllocationAsync(pCode);

            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError(e);
        }
    }
}
