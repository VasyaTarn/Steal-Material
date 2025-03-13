using DG.Tweening;
using System.Collections;
using UniRx;
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

    private float _hostRoundScore = 0;
    private float _clientRoundScore = 0;

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
            _hostFillScore++;
            UIReferencesManager.Instance.HostFillScore.fillAmount = _hostFillScore / _maxScore;
            yield return waitTime;
        }

        _hostRoundScore++;
        UIReferencesManager.Instance.HostRoundScore.text = _hostRoundScore.ToString();

        StartRoundFinisher();
    }

    private IEnumerator FillingClientScore()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.5f);

        while (_clientFillScore != _maxScore)
        {
            _clientFillScore++;
            UIReferencesManager.Instance.ClientFillScore.fillAmount = _clientFillScore / _maxScore;
            yield return waitTime;
        }

        _clientRoundScore++;
        UIReferencesManager.Instance.ClientRoundScore.text = _clientRoundScore.ToString();

        StartRoundFinisher();
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

    public override void OnDestroy()
    {
        //_playerSpawner.OnPlayerHealthControllerChanged -= HandleHealthControllerChanged;

        /*if (_hostHealthController != null && _clientHealthController != null)
        {
            _hostHealthController.OnDeth -= HandlePlayerDeath;
            _clientHealthController.OnDeth -= HandlePlayerDeath;
        }*/

        _disposables.Dispose();
    }
}
