using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UniRx;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

public class GameScoreManager : NetworkBehaviour
{
    [SerializeField] private PlayerSpawner _playerSpawner;
    [SerializeField] private CapturePoint _capturePoint;

    private PlayerHealthController _hostHealthController;
    private PlayerHealthController _clientHealthController;

    private Coroutine _fillingHostScoreCoroutine;
    private Coroutine _fillingClientScoreCoroutine;

    private float _maxScore = 100;

    private NetworkVariable<float> _hostFillScore = new NetworkVariable<float>(0);
    private NetworkVariable<float> _clientFillScore = new NetworkVariable<float>(0);

    private NetworkVariable<int> _hostRoundScore = new NetworkVariable<int>(0);
    private NetworkVariable<int> _clientRoundScore = new NetworkVariable<int>(0);

    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    private void Start()
    {
        _playerSpawner.OnPlayerHealthControllerChanged
            .Subscribe(HandleHealthControllerChanged)
            .AddTo(_disposables);

        //_playerSpawner.OnPlayerHealthControllerChanged += HandleHealthControllerChanged;

        _capturePoint.OnPointCaptured
            .Subscribe(HandlePointCaptured)
            .AddTo(_disposables);

        _capturePoint.OnPointUncaptured
            .Subscribe(HandlePointUncaptured)
            .AddTo(_disposables);

        _hostRoundScore.OnValueChanged += OnHostRoundScoreChanged;
        _clientRoundScore.OnValueChanged += OnClientRoundScoreChanged;

        _hostFillScore.OnValueChanged += OnHostFillScoreChanged;
        _clientFillScore.OnValueChanged += OnClientFillScoreChanged;

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;

        /*_capturePoint.OnPointCaptured += HandlePointCaptured;
        _capturePoint.OnPointUncaptured += HandlePointUncaptured;*/
    }

    private void HandlePointUncaptured(ulong id)
    {
        if (IsServer)
        {
            if (id == 0)
            {
                if (_fillingHostScoreCoroutine != null)
                {
                    StopCoroutine(_fillingHostScoreCoroutine);
                    //StopFillingHostScoreRpc();
                }
            }
            else
            {
                if (_fillingClientScoreCoroutine != null)
                {
                    StopCoroutine(_fillingClientScoreCoroutine);
                    //StopFillingClientScoreRpc();
                }
            }
        }
    }

    private void HandlePointCaptured(ulong invaderId)
    {
        AddScoreByPointServerRpc(invaderId);
    }

    private void HandleHealthControllerChanged(PlayerHealthController newController)
    {
        if (newController.OwnerClientId == 0)
        {
            _hostHealthController = newController;

            _hostHealthController.OnDeath
                .Subscribe(HandlePlayerDeath)
                .AddTo(_disposables);
        }
        else
        {
            _clientHealthController = newController;

            _clientHealthController.OnDeath
                .Subscribe(HandlePlayerDeath)
                .AddTo(_disposables);
        }
        /*if (NetworkManager.Singleton.IsServer)
        {
            if (newController.OwnerClientId == 0)
            {
                _hostHealthController = newController;

                _hostHealthController.OnDeath
                    .Subscribe(HandlePlayerDeath)
                    .AddTo(_disposables);
            }
            else
            {
                _clientHealthController = newController;

                _clientHealthController.OnDeath
                    .Subscribe(HandlePlayerDeath)
                    .AddTo(_disposables);
            }
        }*/
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

        if (_capturePoint.Players.Count > 0)
        {
            _capturePoint.ExitAction(ownerId);
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
    private void AddScoreByPointServerRpc(ulong ownderId)
    {
        if (ownderId == 0)
        {
            _fillingHostScoreCoroutine = StartCoroutine(FillingHostScore());
        }
        else
        {
            _fillingClientScoreCoroutine = StartCoroutine(FillingClientScore());
        }
    }

    private IEnumerator FillingHostScore()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);

        while (_hostFillScore.Value != _maxScore)
        {
            _hostFillScore.Value++;
            yield return waitTime;
        }

        if(IsServer)
        {
            _hostRoundScore.Value++;
        }

        StartRoundFinisherRpc(3f);
    }

    private IEnumerator FillingClientScore()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);

        while (_clientFillScore.Value != _maxScore)
        {
            _clientFillScore.Value++; 
            yield return waitTime;
        }

        if (IsServer)
        {
            _clientRoundScore.Value++;
        }

        StartRoundFinisherRpc(3f);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void StartRoundFinisherRpc(float awaitTime)
    {
        StartCoroutine(FinishRound(awaitTime));
    }

    private IEnumerator FinishRound(float awaitTime)
    {
        yield return new WaitForSeconds(awaitTime);

        UIReferencesManager.Instance.RoundOverScreen.DOFade(1f, 2f);

        yield return new WaitForSeconds(3f);

        if (NetworkManager.Singleton.IsServer)
        {
            _playerSpawner.TeleportHost(NetworkManager.Singleton.LocalClientId);

            yield return new WaitForSeconds(0.2f);
            _playerSpawner.EnablePlayerMovement(NetworkManager.Singleton.LocalClientId, false);

            _capturePoint._units.count.Value = 0;
            _capturePoint.Players.Clear();
            _capturePoint.CaptureProgressBar.ProgressBarImage.fillAmount = _capturePoint._units.count.Value / _maxScore;
        }
        else
        {
            _playerSpawner.TeleportServerRpc(NetworkManager.Singleton.LocalClientId);

            yield return new WaitForSeconds(0.2f);
            _playerSpawner.EnablePlayerMovementServerRpc(NetworkManager.Singleton.LocalClientId, false);
        }

        if (NetworkManager.Singleton.IsServer)
        {
            List<NetworkObjectReference> players = new List<NetworkObjectReference>();

            foreach (var client in NetworkManager.Singleton.ConnectedClients)
            {
                players.Add(client.Value.PlayerObject);
                //client.Value.PlayerObject.GetComponent<SkinContoller>().ChangeSkin(StarterMaterialManager.Instance.GetStarterMaterial());
            }

            ApplyStarterMaterialRpc(players.ToArray());
        }

        yield return new WaitForSeconds(1f);

        if (NetworkManager.Singleton.IsServer)
        {
            _hostFillScore.Value = 0;
            _clientFillScore.Value = 0;
        }


        UIReferencesManager.Instance.TopCapturePointStatus.color = Color.white;

        UIReferencesManager.Instance.RoundOverScreen.DOFade(0f, 1f);

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ApplyStarterMaterialRpc(NetworkObjectReference[] players)
    {
        if(IsServer)
        {
            if (players[0].TryGet(out NetworkObject player))
            {
                player.GetComponent<SkinContoller>().ChangeSkin(StarterMaterialManager.Instance.GetStarterMaterial());
            }
        }
        else
        {
            if (players[1].TryGet(out NetworkObject player))
            {
                player.GetComponent<SkinContoller>().ChangeSkin(StarterMaterialManager.Instance.GetStarterMaterial());
            }
        }
    }

    private void OnHostRoundScoreChanged(int previousValue, int newValue)
    {
        UIReferencesManager.Instance.HostRoundScore.text = newValue.ToString();
    }

    private void OnClientRoundScoreChanged(int previousValue, int newValue)
    {
        UIReferencesManager.Instance.ClientRoundScore.text = newValue.ToString();
    }

    private void OnHostFillScoreChanged(float previousValue, float newValue)
    {
        UIReferencesManager.Instance.HostFillScore.fillAmount = _hostFillScore.Value / _maxScore;
    }

    private void OnClientFillScoreChanged(float previousValue, float newValue)
    {
        UIReferencesManager.Instance.ClientFillScore.fillAmount = _clientFillScore.Value / _maxScore;
    }

    private void OnPlayerConnected(ulong obj)
    {
        if (_hostRoundScore.Value != 0 || _clientRoundScore.Value != 0)
        {
            UIReferencesManager.Instance.HostRoundScore.text = _hostRoundScore.Value.ToString();
            UIReferencesManager.Instance.ClientRoundScore.text = _clientRoundScore.Value.ToString();
        }

        if (IsClient && !IsServer)
        {
            if (_hostFillScore.Value > 0)
            {
                UIReferencesManager.Instance.HostFillScore.fillAmount = _hostFillScore.Value / _maxScore;
            }

            if (_clientFillScore.Value > 0)
            {
                UIReferencesManager.Instance.ClientFillScore.fillAmount = _clientFillScore.Value / _maxScore;
            }
        }

        if (IsServer)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count == 2 && _hostRoundScore.Value == 0 && _clientRoundScore.Value == 0)
            {
                StartRoundFinisherRpc(1f);
                _playerSpawner.HostBarrier.SetActive(true);
            }
        }
    }

    public override void OnDestroy()
    {
        _hostRoundScore.OnValueChanged -= OnHostRoundScoreChanged;
        _clientRoundScore.OnValueChanged -= OnClientRoundScoreChanged;

        _disposables.Dispose();
    }
}
