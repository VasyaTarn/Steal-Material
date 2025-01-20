using Unity.Netcode;
using UnityEngine;

public class MovementStatsNetwork : NetworkBehaviour, IStats
{
    public NetworkVariable<float> moveSpeed = new NetworkVariable<float>(default);
    public NetworkVariable<float> jumpHeight = new NetworkVariable<float>(default);
    public NetworkVariable<float> mouseSensitivity = new NetworkVariable<float>(default);
    public NetworkVariable<bool> isStuned = new NetworkVariable<bool>(default);

    private PlayerMovementController _playerMovementController;


    private void OnEnable()
    {
        moveSpeed.OnValueChanged += OnMoveSpeedChanged;
    }

    private void OnDisable()
    {
        moveSpeed.OnValueChanged -= OnMoveSpeedChanged;
    }

    private void Start()
    {
        _playerMovementController = GetComponent<PlayerMovementController>();
    }

    private void OnMoveSpeedChanged(float previousValue, float newValue)
    {
        if (!IsServer)
        {
            if (_playerMovementController.currentMoveSpeed != newValue)
            {
                _playerMovementController.currentMoveSpeed = newValue;
            }
        }
    }
}
