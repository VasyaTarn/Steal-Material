using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyOccupancyInspector : NetworkBehaviour
{
    private const int _maxPlayers = 2;


    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count > _maxPlayers)
            {
                RequestClientDisconnectRpc(clientId);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RequestClientDisconnectRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            AuthenticationService.Instance.SignOut();
            NetworkManager.Singleton.Shutdown();

            SceneManager.LoadScene("Menu");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            ShowJoinUI().Forget();
        }
    }

    private async UniTaskVoid ShowJoinUI()
    {
        await UniTask.Delay(100);

        JoinUIView joinUIView = GameObject.FindObjectOfType<JoinUIView>();

        if (joinUIView != null)
        {
            joinUIView.HideStartUI();
            joinUIView.DislayJoinUI();
            joinUIView.GetErrorText().text = "Lobby is full";
        }
    }
}
