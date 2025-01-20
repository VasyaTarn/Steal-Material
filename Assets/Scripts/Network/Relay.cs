using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class Relay : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputField;
    [SerializeField] private JoinUIView _joinUIView;
    [SerializeField] private MenuLightController _menuLightController;

    private async void Start()
    {
        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;

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
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(2);
            _menuLightController.DisableLight();

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);


            RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            await Task.Delay(1000);

            SceneManager.LoadScene("Main");

            await Task.Delay(100);

            NetworkManager.Singleton.StartHost();

            CodeDisplayer.displayCode(joinCode);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinRelay()
    {
        if (!string.IsNullOrEmpty(_inputField.text))
        {
            try
            {
                Debug.Log("Join relay with " + _inputField.text);
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(_inputField.text);
                _menuLightController.DisableLight();

                RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
                NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

                await Task.Delay(1000);

                SceneManager.LoadScene("Main");

                NetworkManager.Singleton.StartClient();
            }
            catch (RelayServiceException e)
            {
                Debug.Log(e);
                _joinUIView.DislayJoinUI();
                _joinUIView.GetErrorText().text = "Incorrect code or internet problems";
            }
        }
        else
        {
            await Task.Delay(500);
            _joinUIView.DislayJoinUI();
            _joinUIView.GetErrorText().text = "Code field is empty";
        }
    }

    /*private void OnClientConnected(ulong clientId)
    {
        Debug.Log("Test");
    }*/

}
