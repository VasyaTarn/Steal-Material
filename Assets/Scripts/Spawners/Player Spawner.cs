using System.Collections;
using System.Collections.Generic;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
    }
    
    private void OnPlayerConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(SpawnClient(clientId));
        }
        
        if(NetworkManager.Singleton.IsServer)
        {
            StartCoroutine(SpawnHost(clientId));
        }
    }

    private IEnumerator SpawnHost(ulong clientId)
    {
        yield return new WaitForSeconds(2f);

        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

        playerMovementController.disablingPlayerJumpAndGravity = true;
        playerMovementController.disablingPlayerMove = true;

        if (playerObject != null)
        {
            if (playerObject.IsOwner)
            {
                playerObject.GetComponent<ClientNetworkTransform>().Teleport(hostSpawnPoint.position, playerObject.transform.rotation, playerObject.transform.localScale);
                Debug.Log("TP server");
            }
        }


        yield return new WaitForSeconds(0.2f);

        playerMovementController.disablingPlayerJumpAndGravity = false;
        playerMovementController.disablingPlayerMove = false;
    }

    private IEnumerator SpawnClient(ulong clientId)
    {
        yield return new WaitForSeconds(1f);
        SpawnPlayerOnServerRpc(clientId);

        yield return new WaitForSeconds(0.2f);
        EnablePlayerMovementClientRpc(clientId);
    }

    [Rpc(SendTo.Server)]
    private void SpawnPlayerOnServerRpc(ulong clientId)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (playerObject != null)
        {
            PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

            playerMovementController.disablingPlayerJumpAndGravity = true;
            playerMovementController.disablingPlayerMove = true;

            TeleportClientRpc(clientId, clientSpawnPoint.position);

            /*if (playerObject.IsOwner)
            {
                playerObject.GetComponent<ClientNetworkTransform>().Teleport(clientSpawnPoint.position, playerObject.transform.rotation, playerObject.transform.localScale);
                Debug.Log("TP client");
            }*/
        }

        //Debug.Log($"ID: {clientId} | Test1");
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TeleportClientRpc(ulong clientId, Vector3 position)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (playerObject.IsOwner)
        {
            playerObject.GetComponent<ClientNetworkTransform>().Teleport(position, playerObject.transform.rotation, playerObject.transform.localScale);
            Debug.Log("Test");
        }
    }

    [Rpc(SendTo.Server)]
    private void EnablePlayerMovementClientRpc(ulong clientId)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        if (playerObject != null)
        {
            PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

            playerMovementController.disablingPlayerJumpAndGravity = false;
            playerMovementController.disablingPlayerMove = false;
        }
    }

    /*private IEnumerator SpawnClient(ulong clientId)
    {
        yield return new WaitForSeconds(1f);

        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;

        PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

        playerMovementController.disablingPlayerJumpAndGravity = true;
        playerMovementController.disablingPlayerMove = true;

        if (playerObject != null)
        {
            playerObject.GetComponent<ClientNetworkTransform>().Teleport(clientSpawnPoint.position, playerObject.transform.rotation, playerObject.transform.localScale);
        }

        yield return new WaitForSeconds(0.2f);

        playerMovementController.disablingPlayerJumpAndGravity = false;
        playerMovementController.disablingPlayerMove = false;
    }*/

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        }
    }
}
