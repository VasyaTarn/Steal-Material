using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.Multiplayer.Samples.Utilities.ClientAuthority;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private Transform hostSpawnPoint;
    [SerializeField] private Transform clientSpawnPoint;

    [SerializeField] private GameObject _loadingScreenBackground;
    [SerializeField] private GameObject _loadingIcon;

    private PlayerHealthController _healthController;

    [SerializeField] private GameObject _hostBarrier;
    [SerializeField] private GameObject _clientBarrier;

    public event Action<PlayerHealthController> OnPlayerHealthControllerChanged;

    public PlayerHealthController playerHealthController 
    {
        get => _healthController;
        private set
        {
            if(_healthController != value )
            {
                _healthController = value;
                OnPlayerHealthControllerChanged?.Invoke(_healthController);
            }
        }
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
    }
    
    private void OnPlayerConnected(ulong clientId)
    {
        if (clientId != 0)
        {
            StartCoroutine(SpawnClient(clientId));
        }
        else
        {
            StartCoroutine(SpawnHost(clientId));
        }

        if (NetworkManager.Singleton.IsServer)
        {
            if(NetworkManager.Singleton.ConnectedClientsList.Count > 1)
            {
                StartCoroutine(BarrierTimer(5f));
            }
        }
    }

    private IEnumerator BarrierTimer(float duration)
    {
        yield return new WaitForSeconds(duration);

        DisableBarriersRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void DisableBarriersRpc()
    {
        _hostBarrier.SetActive(false);
        _clientBarrier.SetActive(false);
    }

    private IEnumerator SpawnHost(ulong clientId)
    {
        yield return new WaitForSeconds(1f);

        TeleportHost(clientId);

        yield return new WaitForSeconds(0.2f);

        EnablePlayerMovement(clientId, true);

        yield return new WaitForSeconds(1f);

        _loadingScreenBackground.SetActive(false);
        _loadingIcon.SetActive(false);
    }

    public void TeleportHost(ulong id)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[id].PlayerObject;

        playerHealthController = playerObject.GetComponent<PlayerHealthController>();

        PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

        if (!playerMovementController.disablingPlayerJumpAndGravity)
        {
            playerMovementController.disablingPlayerJumpAndGravity = true;
        }

        if (playerObject != null)
        {
            if (playerObject.IsOwner)
            {
                playerObject.GetComponent<ClientNetworkTransform>().Teleport(hostSpawnPoint.position, playerObject.transform.rotation, playerObject.transform.localScale);
            }
        }
    }

    public void EnablePlayerMovement(ulong id, bool isSpawnMethod)
    {
        NetworkObject playerObject = NetworkManager.Singleton.ConnectedClients[id].PlayerObject;
        PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

        if (!isSpawnMethod)
        {
            StopDeathAnimationClientRpc(playerObject);
        }

        playerMovementController.disablingPlayerJumpAndGravity = false;
        playerMovementController.disablingPlayerMove = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StopDeathAnimationClientRpc(NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerObject))
        {
            PlayerAnimationController playerAnimationController = playerObject.GetComponent<PlayerAnimationController>();
            PlayerHealthController playerHealthController = playerObject.GetComponent<PlayerHealthController>();

            if (playerAnimationController.CachedComponentNetwork.animator.GetCurrentAnimatorStateInfo(playerAnimationController.CachedComponentNetwork.animator.GetLayerIndex("Movement")).IsName("Death"))
            {
                playerAnimationController.CachedComponentNetwork.animator.SetBool("IsDeath", false);
                playerAnimationController.CachedComponentNetwork.isEnabledAnimatorIK = true;
            }

            playerHealthController.SetMaxHpServerRpc();
        }
    }

    private IEnumerator SpawnClient(ulong clientId)
    {
        yield return new WaitForSeconds(1f);
        TeleportServerRpc(clientId);

        yield return new WaitForSeconds(0.5f);
        EnablePlayerMovementServerRpc(clientId, true);
    }

    [Rpc(SendTo.Server)]
    public void TeleportServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject != null)
            {
                PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

                if (!playerMovementController.disablingPlayerJumpAndGravity)
                {
                    playerMovementController.disablingPlayerJumpAndGravity = true;
                }

                TeleportClientRpc(playerObject, clientSpawnPoint.position);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TeleportClientRpc(NetworkObjectReference playerReference, Vector3 position)
    {
        if (playerReference.TryGet(out NetworkObject playerObject))
        {
            playerHealthController = playerObject.GetComponent<PlayerHealthController>();

            if (playerObject.IsOwner)
            {
                PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

                playerObject.GetComponent<ClientNetworkTransform>().Teleport(position, playerObject.transform.rotation, playerObject.transform.localScale);
            }
        }
    }

    [Rpc(SendTo.Server)]
    public void EnablePlayerMovementServerRpc(ulong clientId, bool isSpawnMethod)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            NetworkObject playerObject = client.PlayerObject;

            if (playerObject != null)
            {
                EnablePlayerMovementClientRpc(playerObject, isSpawnMethod);
            }
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EnablePlayerMovementClientRpc(NetworkObjectReference playerReference, bool isSpawnMethod)
    {
        if (playerReference.TryGet(out NetworkObject playerObject))
        {
            if (playerObject.IsOwner)
            {
                PlayerMovementController playerMovementController = playerObject.GetComponent<PlayerMovementController>();

                if (!isSpawnMethod)
                {
                    PlayerAnimationController playerAnimationController = playerObject.GetComponent<PlayerAnimationController>();
                    PlayerHealthController playerHealthController = playerObject.GetComponent<PlayerHealthController>();

                    if (playerAnimationController.SkinView.CurrentArmatureLocal.animator.GetCurrentAnimatorStateInfo(playerAnimationController.SkinView.CurrentArmatureLocal.animator.GetLayerIndex("Movement")).IsName("Death"))
                    {
                        playerAnimationController.SkinView.CurrentArmatureLocal.animator.SetBool("IsDeath", false);
                        playerAnimationController.SkinView.CurrentArmatureLocal.isEnabledAnimatorIK = true;
                    }

                    playerHealthController.SetMaxHpServerRpc();
                    StopDeathAnimationServerRpc(playerObject);
                }

                playerMovementController.disablingPlayerJumpAndGravity = false;
                playerMovementController.disablingPlayerMove = false;

                if (_loadingScreenBackground.activeSelf && _loadingIcon.activeSelf)
                {
                    StartCoroutine(DisableLoadingScreen());
                }
            }
        }
    }

    private IEnumerator DisableLoadingScreen()
    {
        yield return new WaitForSeconds(1f);

        _loadingScreenBackground.SetActive(false);
        _loadingIcon.SetActive(false);
    }

    [Rpc(SendTo.Server)]
    private void StopDeathAnimationServerRpc(NetworkObjectReference playerReference)
    {
        if (playerReference.TryGet(out NetworkObject playerObject))
        {
            PlayerAnimationController playerAnimationController = playerObject.GetComponent<PlayerAnimationController>();
            PlayerHealthController playerHealthController = playerObject.GetComponent<PlayerHealthController>();

            if (playerAnimationController.CachedComponentNetwork.animator.GetCurrentAnimatorStateInfo(playerAnimationController.CachedComponentNetwork.animator.GetLayerIndex("Movement")).IsName("Death"))
            {
                playerAnimationController.CachedComponentNetwork.animator.SetBool("IsDeath", false);
                playerAnimationController.CachedComponentNetwork.isEnabledAnimatorIK= true;
            }

            playerHealthController.SetMaxHpServerRpc();
        }
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        }
    }
}
