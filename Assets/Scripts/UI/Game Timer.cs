using Cysharp.Threading.Tasks;
using System;
using TMPro;
using UniRx;
using Unity.Netcode;
using UnityEngine;

public class GameTimer : NetworkBehaviour
{
    [SerializeField] private TMP_Text _minuteTimer;
    [SerializeField] private TMP_Text _secondTimer;

    [SerializeField] private CapturePoint _capturePoint;

    private NetworkVariable<int> _minuteTicks = new NetworkVariable<int>(0);
    private NetworkVariable<int> _secondTicks = new NetworkVariable<int>(0);

    private IDisposable _timerSubscription;

    private CompositeDisposable _disposable = new CompositeDisposable();

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
    }

    private void OnPlayerConnected(ulong obj)
    {
        if(NetworkManager.Singleton.IsServer)
        {
            if(NetworkManager.ConnectedClients.Count > 1)
            {
                if (_timerSubscription == null)
                {
                    _timerSubscription = Observable
                        .Interval(TimeSpan.FromSeconds(1f))
                        .Subscribe(_ => AddTick())
                        .AddTo(_disposable);
                }
            }
        }
    }

    private void AddTick()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        _secondTicks.Value++;

        UpdateSecondTimerRpc(_secondTicks.Value);

        if (_secondTicks.Value == 10)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count > 1)
            {
                _capturePoint.ActivatePointRpc();
            }
        }

        if (_secondTicks.Value >= 60)
        {
            _secondTicks.Value = 0;
            UpdateSecondTimerRpc(_secondTicks.Value);

            _minuteTicks.Value++;
            UpdateMinuteTimerRpc(_minuteTicks.Value);
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateMinuteTimerRpc(int minutes)
    {
        if(minutes < 10)
        {
            _minuteTimer.text = "0" + minutes.ToString();
        }
        else
        {
            _minuteTimer.text = minutes.ToString();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateSecondTimerRpc(int seconds)
    {
        if (seconds < 10)
        {
            _secondTimer.text = "0" + seconds.ToString();
        }
        else
        {
            if (seconds != 60)
            {
                _secondTimer.text = seconds.ToString();
            }
        }
    }

    public override void OnDestroy()
    {
        _disposable.Dispose();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        }
    }
}
