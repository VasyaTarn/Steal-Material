using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameScoreManager : NetworkBehaviour
{
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private CapturePoint _capturePoint;

    private PlayerHealthController _hostHealthController;
    private PlayerHealthController _clientHealthController;

    private Coroutine _fillingHostScoreCoroutine;
    private Coroutine _fillingClientScoreCoroutine;

    private float _maxScore = 100;

    private float _hostScore = 0;
    private float _clientScore = 0;

    private void Start()
    {
        _playerSpawner.OnPlayerHealthControllerChanged += HandleHealthControllerChanged;
        _capturePoint.OnPointCaptured += HandlePointCaptured;
        _capturePoint.OnPointUncaptured += HandlePointUncaptured;
    }

    private void HandlePointUncaptured(ulong id)
    {
        if (id == 0)
        {
            StopFillingHostScoreRpc();
        }
        else
        {
            StopFillingClientScoreRpc();
        }
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
        _fillingClientScoreCoroutine = StartCoroutine(FillingClientScore());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeUIHostScoreRpc()
    {
        _fillingHostScoreCoroutine = StartCoroutine(FillingHostScore());
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StopFillingHostScoreRpc()
    {
        StopCoroutine(_fillingHostScoreCoroutine);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StopFillingClientScoreRpc()
    {
        StopCoroutine(_fillingClientScoreCoroutine);
    }

    private IEnumerator FillingHostScore()
    {
        while (_hostScore != _maxScore)
        {
            _hostScore += 20;
            UIReferencesManager.Instance.HostScore.fillAmount = _hostScore / _maxScore;
            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator FillingClientScore()
    {
        while (_clientScore != _maxScore)
        {
            _hostScore += 20;
            UIReferencesManager.Instance.ClientScore.fillAmount = _clientScore / _maxScore;
            yield return new WaitForSeconds(0.5f);
        }
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
