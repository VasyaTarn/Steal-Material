using Unity.Netcode;
using UnityEngine;

public class MovementStatsNetwork : NetworkBehaviour, IStats
{
    public NetworkVariable<float> moveSpeed = new NetworkVariable<float>(default);
    public NetworkVariable<float> jumpHeight = new NetworkVariable<float>(default);
    public NetworkVariable<float> mouseSensitivity = new NetworkVariable<float>(default);
    public NetworkVariable<bool> isStuned = new NetworkVariable<bool>(default);

    private PlayerMovementController playerMovementController;

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
        playerMovementController = GetComponent<PlayerMovementController>();
    }

    private void OnMoveSpeedChanged(float previousValue, float newValue)
    {
        if (!IsServer)
        {
            if (playerMovementController.currentMoveSpeed != newValue)
            {
                playerMovementController.currentMoveSpeed = newValue;
            }
        }
    }
}
