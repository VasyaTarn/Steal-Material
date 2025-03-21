using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

enum PointWaveType : byte
{
    Blue,
    Red
}

public class CapturePoint : NetworkBehaviour
{
    private Dictionary<ulong, GameObject> _players = new Dictionary<ulong, GameObject>();
    public CaptureUnit _units;

    private float _maxScore = 100f;
    private float _minScore = 0f;

    private Coroutine _increaseCorotine;
    private Coroutine _decreaseCorotine;

    private GameObject _pointWaveBlue;
    private GameObject _pointWaveRed; 

    private bool _isLockUp = true;

    private readonly Subject<ulong> _onPointCapturedSubject = new Subject<ulong>();
    public IObservable<ulong> OnPointCaptured => _onPointCapturedSubject;

    private readonly Subject<ulong> _onPointUncapturedSubject = new Subject<ulong>();
    public IObservable<ulong> OnPointUncaptured => _onPointUncapturedSubject;

    /*public event Action<ulong> OnPointCaptured;
    public event Action<ulong> OnPointUncaptured;*/

    [SerializeField] private CaptureProgressBar _captureProgressBar;

    public CaptureProgressBar CaptureProgressBar => _captureProgressBar;

    private void Start()
    {
        if (!IsServer)
        {
            _captureProgressBar.ProgressBarImage.color = Color.blue;
        }

        Addressables.LoadAssetAsync<GameObject>("Point_Wave_Blue").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _pointWaveBlue = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Point_Wave_Blue");
            }
        };

        Addressables.LoadAssetAsync<GameObject>("Point_Wave_Red").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _pointWaveRed = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Point_Wave_Red");
            }
        };

        /*_pointWaveBlue = Resources.Load<GameObject>("General/Point_Wave_Blue");
        _pointWaveRed = Resources.Load<GameObject>("General/Point_Wave_Red");*/

        _units.count.OnValueChanged += HandleCountChanged;

        _units.ownerId.OnValueChanged += HandleOwnerChanged;

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;

        //NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void HandleOwnerChanged(ulong previousValue, ulong newValue)
    {
        _captureProgressBar.ProgressBarImage.color = (newValue == 0) ? Color.blue : Color.red;
    }

    private void HandleCountChanged(float previousValue, float newValue)
    {
        _captureProgressBar.ProgressBarImage.fillAmount = newValue / _maxScore;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        ulong ownerId = other.GetComponent<NetworkObject>().OwnerClientId;

        _players.Add(ownerId, other.gameObject);

        if (_units.count.Value == 0)
        {
            _units.ownerId.Value = ownerId;
            _increaseCorotine = StartCoroutine(IncreasePoints());
        }
        else if(_units.count.Value > 0 && _units.ownerId.Value != ownerId)
        {
            _decreaseCorotine = StartCoroutine(DecreasePoints());
        }
        else if (_units.count.Value > 0 && _units.ownerId.Value == ownerId && _units.count.Value < _maxScore)
        {
            _increaseCorotine = StartCoroutine(IncreasePoints());
        }

        if (_players.Count == 2)
        {
            StopCoroutine(_increaseCorotine);
            StopCoroutine(_decreaseCorotine);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        ulong ownerId = other.GetComponent<NetworkObject>().OwnerClientId;

        _players.Remove(ownerId);

        if(_increaseCorotine != null && _players.Count == 0)
        {
            StopCoroutine(_increaseCorotine);

            if (_decreaseCorotine != null)
            {
                StopCoroutine(_decreaseCorotine);
            }
        }

        if(_players.Count == 1)
        {
            if(ownerId != _units.ownerId.Value)
            {
                _increaseCorotine = StartCoroutine(IncreasePoints());
            }
            else
            {
                _decreaseCorotine = StartCoroutine(DecreasePoints());
            }
        }
    }

    private IEnumerator IncreasePoints()
    {
        if (IsServer)
        {
            _captureProgressBar.ProgressBarImage.color = (_units.ownerId.Value == 0) ? Color.blue : Color.red;
        }

        WaitForSeconds waitTime = new WaitForSeconds(0.2f);

        while (_units.count.Value < _maxScore)
        {
            if (!_isLockUp)
            {
                IncrementCountServerRpc();

                /*if (_units.count.Value == _maxScore / 2)
                {
                    StartCoroutine(LockUpPoint(3f));
                }*/
            }

            yield return waitTime;
        }

        if(_units.count.Value == _maxScore)
        {
            //OnPointCaptured?.Invoke(_units.ownerId.Value);
            _onPointCapturedSubject.OnNext(_units.ownerId.Value);

            if(_units.ownerId.Value == 0)
            {
                SpawnPointWaveServerRpc(transform.position, _units.ownerId.Value, PointWaveType.Blue);
            }
            else
            {
                SpawnPointWaveServerRpc(transform.position, _units.ownerId.Value, PointWaveType.Red);
            }

            SetTopCapturePointStatusRpc(_units.ownerId.Value);
        }    
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetTopCapturePointStatusRpc(ulong ownerId)
    {
        if(ownerId == 0)
        {
            UIReferencesManager.Instance.TopCapturePointStatus.color = Color.blue;
        }
        else
        {
            UIReferencesManager.Instance.TopCapturePointStatus.color = Color.red;
        }
    }

    [Rpc(SendTo.Server)]
    private void IncrementCountServerRpc()
    {
        if (_units.count.Value < _maxScore)
        {
            _units.count.Value += 20; //

            _captureProgressBar.ProgressBarImage.fillAmount = _units.count.Value / _maxScore;
        }
    }

    /*private IEnumerator LockUpPoint(float duration)
    {
        LockUpPointClientRpc(true);
        _isLockUp = true;

        yield return new WaitForSeconds(duration);

        LockUpPointClientRpc(false);
        _isLockUp = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LockUpPointClientRpc(bool isLocked)
    {
        _captureProgressBar.LockImage.SetActive(isLocked);
    }*/

    [Rpc(SendTo.ClientsAndHost)]
    public void ActivatePointRpc()
    {
        _captureProgressBar.LockImage.SetActive(false);

        _isLockUp = false;
    }

    private IEnumerator DecreasePoints()
    {
        WaitForSeconds waitTime = new WaitForSeconds(0.2f);

        while (_units.count.Value > 0)
        {
            DecrementCountServerRpc();
            yield return waitTime;
        }

        //OnPointUncaptured?.Invoke(_units.ownerId.Value);
        _onPointUncapturedSubject.OnNext(_units.ownerId.Value);

        if (_players.Count > _minScore)
        {
            SetNewOwnerServerRpc(_players.Keys.FirstOrDefault());
            _increaseCorotine = StartCoroutine(IncreasePoints());
        }
    }

    [Rpc(SendTo.Server)]
    private void DecrementCountServerRpc()
    {
        if (_units.count.Value > 0)
        {
            _units.count.Value -= 20; //
            _captureProgressBar.ProgressBarImage.fillAmount = _units.count.Value / _maxScore;
        }
    }

    [Rpc(SendTo.Server)]
    private void SetNewOwnerServerRpc(ulong newOwnerId)
    {
        _units.ownerId.Value = newOwnerId;
    }

    [Rpc(SendTo.Server)]
    private void SpawnPointWaveServerRpc(Vector3 pointWaveSpawnPoint, ulong ownerId, PointWaveType type)
    {
        if (type == PointWaveType.Blue)
        {
            NetworkObject pointWaveBlueNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_pointWaveBlue, pointWaveSpawnPoint);
            pointWaveBlueNetwork.Spawn();

            StartCoroutine(ReleasePointWave(5f, () =>
            {
                if (IsServer)
                {
                    if (pointWaveBlueNetwork.IsSpawned)
                    {
                        pointWaveBlueNetwork.Despawn();
                    }
                }
            }));
        }
        else if (type == PointWaveType.Red)
        {
            NetworkObject pointWaveRedNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_pointWaveRed, pointWaveSpawnPoint);
            pointWaveRedNetwork.Spawn();

            StartCoroutine(ReleasePointWave(5f, () =>
            {
                if (IsServer)
                {
                    if (pointWaveRedNetwork.IsSpawned)
                    {
                        pointWaveRedNetwork.Despawn();
                    }
                }
            }));
        }
    }

    private IEnumerator ReleasePointWave(float duration, Action releaseAction)
    {
        yield return new WaitForSeconds(duration);

        releaseAction?.Invoke();
    }

    private void OnPlayerConnected(ulong obj)
    {
        if (IsClient && !IsServer)
        {
            if (_units.count.Value > 0)
            {
                _captureProgressBar.ProgressBarImage.fillAmount = _units.count.Value / _maxScore;
            }
        }

        if(IsServer)
        {
            UpdateCaptureLockUIRpc();
            _isLockUp = false;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateCaptureLockUIRpc()
    {
        _captureProgressBar.LockImage.SetActive(false);
        _captureProgressBar.ProgressBarImage.color = (_units.ownerId.Value == 0) ? Color.blue : Color.red;

    }

    private void OnPlayerDisconnected(ulong obj)
    {
        if (IsServer)
        {
            _captureProgressBar.LockImage.SetActive(true);

            _isLockUp = true;

            Debug.Log("TEST Disconnect");
        }
    }

    public override void OnDestroy()
    {
        _units.count.OnValueChanged -= HandleCountChanged;
        _units.ownerId.OnValueChanged -= HandleOwnerChanged;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;

        /*if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }*/
    }
}
