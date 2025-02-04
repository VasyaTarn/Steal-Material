using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CapturePoint : NetworkBehaviour
{
    private Dictionary<ulong, GameObject> _players = new Dictionary<ulong, GameObject>();
    [SerializeField] private CaptureUnit _units;

    private float _maxScore = 100f;
    private float _minScore = 0f;

    private Coroutine _increaseCorotine;
    private Coroutine _decreaseCorotine;

    private bool _isLockUp = false;

    [SerializeField] private CaptureProgressBar _captureProgressBar;

    private void Start()
    {
        if (!IsServer)
        {
            _captureProgressBar.ProgressBarImage.color = Color.blue;
        }

        _units.count.OnValueChanged += (oldValue, newValue) =>
        {
            _captureProgressBar.ProgressBarImage.fillAmount = newValue / _maxScore;
        };

        _units.ownerId.OnValueChanged += (oldOwner, newOwner) =>
        {
            _captureProgressBar.ProgressBarImage.color = (newOwner == 0) ? Color.blue : Color.red;
        };
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
        else if (_units.count.Value > 0 && _units.ownerId.Value == ownerId)
        {
            _increaseCorotine = StartCoroutine(IncreasePoints());
        }

        if (_players.Count == 2)
        {
            StopAllCoroutines();
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

        while (_units.count.Value < _maxScore)
        {
            if (!_isLockUp)
            {
                IncrementCountServerRpc();

                if (_units.count.Value == _maxScore / 2)
                {
                    StartCoroutine(LockUpPoint(3f));
                }
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    [Rpc(SendTo.Server)]
    private void IncrementCountServerRpc()
    {
        if (_units.count.Value < _maxScore)
        {
            _units.count.Value++;

            _captureProgressBar.ProgressBarImage.fillAmount = _units.count.Value / _maxScore;
        }
    }

    private IEnumerator LockUpPoint(float duration)
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
    }

    private IEnumerator DecreasePoints()
    {
        while (_units.count.Value > 0)
        {
            DecrementCountServerRpc();
            yield return new WaitForSeconds(0.2f);
        }

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
            _units.count.Value--;
            _captureProgressBar.ProgressBarImage.fillAmount = _units.count.Value / _maxScore;
        }
    }

    [Rpc(SendTo.Server)]
    private void SetNewOwnerServerRpc(ulong newOwnerId)
    {
        _units.ownerId.Value = newOwnerId;
    }
}
