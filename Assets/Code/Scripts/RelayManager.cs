using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Transports.UTP;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    private const string KEY_RELAY = "Relay";
    static public async void CreateRelay(Lobby pLobby)
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
                            { KEY_RELAY, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
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

    static public async void JoinRelay(string pCode)
    {
        try
        {
            var allocation = await RelayService.Instance.JoinAllocationAsync(pCode);

            var relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }
}
