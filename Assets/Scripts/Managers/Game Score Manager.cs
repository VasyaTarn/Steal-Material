using DG.Tweening;
using System;
using System.Collections;
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

    private float _hostFillScore = 0;
    private float _clientFillScore = 0;

    public NetworkVariable<int> _hostRoundScore = new NetworkVariable<int>(0);
    public NetworkVariable<int> _clientRoundScore = new NetworkVariable<int>(0);

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

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;

        /*_capturePoint.OnPointCaptured += HandlePointCaptured;
        _capturePoint.OnPointUncaptured += HandlePointUncaptured;*/
    }

    private void HandlePointUncaptured(ulong id)
    {
        if (id == 0)
        {
            if (_fillingHostScoreCoroutine != null)
            {
                StopFillingHostScoreRpc();
            }
        }
        else
        {
            if (_fillingClientScoreCoroutine != null)
            {
                StopFillingClientScoreRpc();
            }
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
                //_hostHealthController.OnDeth += HandlePlayerDeath;

                _hostHealthController.OnDeath
                    .Subscribe(HandlePlayerDeath)
                    .AddTo(_disposables);
            }
            else
            {
                _clientHealthController = newController;
                //_clientHealthController.OnDeth += HandlePlayerDeath;

                _clientHealthController.OnDeath
                    .Subscribe(HandlePlayerDeath)
                    .AddTo(_disposables);
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
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);

        while (_hostFillScore != _maxScore)
        {
            _hostFillScore += 20; //
            UIReferencesManager.Instance.HostFillScore.fillAmount = _hostFillScore / _maxScore;
            yield return waitTime;
        }

        if(IsServer)
        {
            _hostRoundScore.Value++;
        }

        StartRoundFinisher();
    }

    private IEnumerator FillingClientScore()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);

        while (_clientFillScore != _maxScore)
        {
            _clientFillScore += 20; //
            UIReferencesManager.Instance.ClientFillScore.fillAmount = _clientFillScore / _maxScore;
            yield return waitTime;
        }

        if (IsServer)
        {
            _clientRoundScore.Value++;
        }

        StartRoundFinisher();
    }

    [Rpc(SendTo.Server)]
    private void AddRoundScoreRpc(bool isHost)
    {
        Debug.Log("Test add");

        if(isHost)
        {
            _hostRoundScore.Value++;
            UpdateRoundScoreUIRpc(isHost);
        }
        else
        {
            _clientRoundScore.Value++;
            UpdateRoundScoreUIRpc(isHost);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateRoundScoreUIRpc(bool isHost)
    {
        if (isHost)
        {
            Debug.Log("Update host score");
            UIReferencesManager.Instance.HostRoundScore.text = _hostRoundScore.Value.ToString();
        }
        else
        {
            Debug.Log("Update client score");
            UIReferencesManager.Instance.ClientRoundScore.text = _clientRoundScore.Value.ToString();
        }
    }

    private void StartRoundFinisher()
    {
        StartCoroutine(FinishRound());
    }

    private IEnumerator FinishRound()
    {
        yield return new WaitForSeconds(3f);

        UIReferencesManager.Instance.RoundOverScreen.DOFade(1f, 2f);

        yield return new WaitForSeconds(3f);

        if(NetworkManager.Singleton.IsServer)
        {
            _playerSpawner.TeleportHost(NetworkManager.Singleton.LocalClientId);

            yield return new WaitForSeconds(0.2f);
            _playerSpawner.EnablePlayerMovement(NetworkManager.Singleton.LocalClientId, false);

            _capturePoint._units.count.Value = 0;
            _capturePoint.CaptureProgressBar.ProgressBarImage.fillAmount = _capturePoint._units.count.Value / _maxScore;
        }
        else
        {
            _playerSpawner.TeleportServerRpc(NetworkManager.Singleton.LocalClientId);

            yield return new WaitForSeconds(0.2f);
            _playerSpawner.EnablePlayerMovementServerRpc(NetworkManager.Singleton.LocalClientId, false);
        }

        yield return new WaitForSeconds(1f);

        _hostFillScore = 0;
        UIReferencesManager.Instance.HostFillScore.fillAmount = _hostFillScore / _maxScore;

        _clientFillScore = 0;
        UIReferencesManager.Instance.ClientFillScore.fillAmount = _clientFillScore / _maxScore;

        UIReferencesManager.Instance.TopCapturePointStatus.color = Color.white;

        UIReferencesManager.Instance.RoundOverScreen.DOFade(0f, 1f);

    }

    private void OnHostRoundScoreChanged(int previousValue, int newValue)
    {
        UIReferencesManager.Instance.HostRoundScore.text = newValue.ToString();
    }

    private void OnClientRoundScoreChanged(int previousValue, int newValue)
    {
        UIReferencesManager.Instance.ClientRoundScore.text = newValue.ToString();
    }

    private void OnPlayerConnected(ulong obj)
    {
        if (_hostRoundScore.Value != 0 || _clientRoundScore.Value != 0)
        {
            UIReferencesManager.Instance.HostRoundScore.text = _hostRoundScore.Value.ToString();
            UIReferencesManager.Instance.ClientRoundScore.text = _clientRoundScore.Value.ToString();
        }
    }

    public override void OnDestroy()
    {
        //_playerSpawner.OnPlayerHealthControllerChanged -= HandleHealthControllerChanged;

        /*if (_hostHealthController != null && _clientHealthController != null)
        {
            _hostHealthController.OnDeth -= HandlePlayerDeath;
            _clientHealthController.OnDeth -= HandlePlayerDeath;
        }*/

        _hostRoundScore.OnValueChanged -= OnHostRoundScoreChanged;
        _clientRoundScore.OnValueChanged -= OnClientRoundScoreChanged;

        _disposables.Dispose();
    }
}
