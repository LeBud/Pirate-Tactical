using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayScript : MonoBehaviour
{
    /*private async void Start()
    {
    //Comme cette fonction est déjà appelé dans le lobby script je ne pense pas en avoir besoin ici
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () => { };
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }*/

    public static RelayScript Instance { get; private set; }

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1); //Le chiffre détermine combien de joueur peuvent être connecté (l'host n'est pas compté)
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log(joinCode);

            RelayServerData data = new RelayServerData(allocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(data);

            NetworkManager.Singleton.StartHost();
            HUD.Instance.inGameHUD.SetActive(true);
            GridManager.Instance.GenerateGridOnTileMapServerRpc();

            return joinCode;
        }
        catch(RelayServiceException e)
        {
            Debug.Log(e);
            return null;
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData data = new RelayServerData(joinAllocation, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(data);

            NetworkManager.Singleton.StartClient();
            HUD.Instance.inGameHUD.SetActive(true);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

}
