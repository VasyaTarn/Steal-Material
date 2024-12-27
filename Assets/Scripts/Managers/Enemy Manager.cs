using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnemyManager : NetworkBehaviour
{
    public List<GameObject> players = new List<GameObject>();

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
    }

    private void onDestroy()
    {
        if(NetworkManager.Singleton == null)
        {
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
    }

    private void OnPlayerConnected(ulong clientId)
    {

        if(IsServer)
        {
            GameObject playerObject = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.gameObject;

            if (playerObject != null)
            {
                players.Add(playerObject);
            }

            if (players.Count == 2)
            {
                assignEnemies(players[0], players[1]);
            }

        }
    }
    
    private void OnPlayerDisconnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            var playerObject = client.PlayerObject.gameObject;
            players.Remove(playerObject);
        }
    }

    private void assignEnemies(GameObject player1, GameObject player2)
    {
        PlayerSkillsController playerController1 = player1.GetComponent<PlayerSkillsController>();
        PlayerSkillsController playerController2 = player2.GetComponent<PlayerSkillsController>();

        if(playerController1 != null && playerController2 != null)
        {
            playerController1.setEnemy(player2);
            playerController2.setEnemy(player1);

            setEnemyClientRpc(player1.GetComponent<NetworkObject>().NetworkObjectId, player2.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    [ClientRpc]
    private void setEnemyClientRpc(ulong player1Id, ulong player2Id)
    {
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(player1Id, out NetworkObject player1Object);
        NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(player2Id, out NetworkObject player2Object);

        if (player1Object != null && player2Object != null)
        {
            PlayerSkillsController playerController1 = player1Object.GetComponent<PlayerSkillsController>();
            PlayerSkillsController playerController2 = player2Object.GetComponent<PlayerSkillsController>();

            if (playerController1 != null && playerController2 != null)
            {
                playerController1.setEnemy(player2Object.gameObject);
                playerController2.setEnemy(player1Object.gameObject);
            }
        }
    }

}
