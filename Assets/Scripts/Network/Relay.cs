using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;

public class Relay : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;



    private async void Start()
    {
        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

        inputField.onEndEdit.AddListener(JoinRelay);

        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateRelay()
    {
        try
        {
            //SceneManager.LoadScene("Main");

            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);


            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            CodeDisplayer.displayCode(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            //SceneManager.LoadScene("Main");

            Debug.Log("Join relay with " + joinCode);
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    /* private void OnClientConnected(ulong clientId)
     {
         if (clientId == NetworkManager.Singleton.LocalClientId && NetworkManager.Singleton.IsClient)
         {
             Debug.Log("Client connected: " + clientId);

             GameObject playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId).gameObject;

             // playerObject.GetComponent<PlayerController>().Initialize(clientId);
         }
     }*/

}
