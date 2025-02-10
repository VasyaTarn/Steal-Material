using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameScoreManager : NetworkBehaviour
{
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private CapturePoint _capturePoint;

    private PlayerHealthController _hostHealthController;
    private PlayerHealthController _clientHealthController;

    private int _hostScore = 0;
    private int _clientScore = 0;

    private void Start()
    {
        _playerSpawner.OnPlayerHealthControllerChanged += HandleHealthControllerChanged;
        _capturePoint.OnPointCaptured += HandlePointCaptured;
    }

    private void HandlePointCaptured(ulong invaderId)
    {
        AddScoreByPointServerRpc(invaderId);
    }

    private void HandleHealthControllerChanged(PlayerHealthController newController)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            if (newController.OwnerClientId == 0)
            {
                _hostHealthController = newController;
                _hostHealthController.OnDeth += HandlePlayerDeath;
            }
            else
            {
                _clientHealthController = newController;
                _clientHealthController.OnDeth += HandlePlayerDeath;
            }
        }
    }

    private void HandlePlayerDeath(ulong ownerId)
    {
        if (ownerId == 0)
        {
            StartCoroutine(TeleportHost(ownerId));
        }
        else
        {
            StartCoroutine(TeleportClient(ownerId));
        }
    }

    private IEnumerator TeleportHost(ulong clientId)
    {
        yield return new WaitForSeconds(5f);
        _playerSpawner.TeleportHost(clientId);

        yield return new WaitForSeconds(0.2f);
        _playerSpawner.EnablePlayerMovement(clientId, false);
    }

    private IEnumerator TeleportClient(ulong clientId)
    {
        yield return new WaitForSeconds(5f);
        _playerSpawner.TeleportServerRpc(clientId);

        yield return new WaitForSeconds(0.2f);
        _playerSpawner.EnablePlayerMovementServerRpc(clientId, false);
    }

    [Rpc(SendTo.Server)]
    private void AddScoreByDeathServerRpc(ulong ownderId)
    {
        if (ownderId == 0)
        {
            ChangeUIClientScoreRpc();
        }
        else
        {
            ChangeUIHostScoreRpc();
        }
    }

    [Rpc(SendTo.Server)]
    private void AddScoreByPointServerRpc(ulong ownderId)
    {
        if (ownderId == 0)
        {
            ChangeUIHostScoreRpc();
        }
        else
        {
            ChangeUIClientScoreRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeUIClientScoreRpc()
    {
        _clientScore++;
        UIReferencesManager.Instance.ClientScore.text = _clientScore.ToString();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeUIHostScoreRpc()
    {
        _hostScore++;
        UIReferencesManager.Instance.HostScore.text = _hostScore.ToString();
    }

    public override void OnDestroy()
    {
        _playerSpawner.OnPlayerHealthControllerChanged -= HandleHealthControllerChanged;

        if (_hostHealthController != null && _clientHealthController != null)
        {
            _hostHealthController.OnDeth -= HandlePlayerDeath;
            _clientHealthController.OnDeth -= HandlePlayerDeath;
        }
    }
}
