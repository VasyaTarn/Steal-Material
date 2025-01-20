using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.Netcode;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerMovementController : NetworkBehaviour
{
    [Header("Player")]
    public MovementStatsNetwork currentMovementStats;

    [HideInInspector] public float currentMoveSpeed;

    [Range(0.0f, 0.3f)]
    public float rotationSmoothTime = 0.05f;

    public float speedChangeRate = 3.0f;

    [Space(20)]
    public float gravity = -15f;

    public float fallTimeout = 0.15f;

    [Space(20)]
    [Header("Stats")]
    public MovementStatsLocal baseMovementStats;
    public bool isSwitchToNetworkSpeed = false;

    [Space(20)]
    [Header("Player Grounded")]
    public bool grounded = true;

    public float groundedOffset = 0.6f;

    public float groundedRadius = 0.5f;

    public LayerMask groundLayers;

    [Space(20)]
    [Header("Cinemachine")]
    public GameObject cinemachineCameraTarget;

    public float topClamp = 70.0f;

    public float bottomClamp = -30.0f;

    public float cameraAngleOverride = 0.0f;

    public Vector2 look;

    private float _speed;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    public float verticalVelocity;
    private float _terminalVelocity = 53.0f;
    [HideInInspector] public Vector3 characterVelocityMomentum;
    [HideInInspector] public Vector3 characterVelocity;
    [HideInInspector] public float momentumDrag = 3f;
    private Vector3 _targetDirection;

    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;

    private float _fallTimeoutDelta;

    private const float _threshold = 0.01f;

    private PlayerInput _playerInput;
    [HideInInspector] public CharacterController controller;
    [HideInInspector] public Inputs inputs;
    [HideInInspector] public Camera mainCamera;
    private GameObject _startCamera;
    private SkinContoller _skinContoller;
    private BoxCollider _boxCollider;

    [HideInInspector] public StatusEffectsController<MovementStatsNetwork, MovementStatsLocal> statusEffectsController;

    //private bool rotateOnMove = true;
    //private bool isMovementDisabled = false;

    //public bool test = false;

    private bool IsCurrentDeviceMouse
    {
        get
        {
            return _playerInput.currentControlScheme == "KeyboardMouse";
        }
    }

    private void Awake()
    {
        Camera camera = GetComponentInChildren<Camera>();
        mainCamera = camera;

        Application.targetFrameRate = 60;
    }

    private void Start()
    {
        if (!IsOwner)
        {
            mainCamera.gameObject.SetActive(false);
            return;
        }

        SetMovementStatsRpc();

        _startCamera = GameObject.Find("Start Camera");

        if(_startCamera != null)
        {
            _startCamera.SetActive(false);
        }

        Cursor.visible = false;

        Cursor.lockState = CursorLockMode.Locked;

        _cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _playerInput = GetComponent<PlayerInput>();
        controller = GetComponent<CharacterController>();
        inputs = GetComponent<Inputs>();
        _skinContoller = GetComponent<SkinContoller>();

        currentMoveSpeed = baseMovementStats.moveSpeed;

        _fallTimeoutDelta = fallTimeout;
    }

    [Rpc(SendTo.Server)]
    private void SetMovementStatsRpc()
    {
        currentMovementStats.moveSpeed.Value = baseMovementStats.moveSpeed;
        currentMovementStats.jumpHeight.Value = baseMovementStats.jumpHeight;
        currentMovementStats.mouseSensitivity.Value = baseMovementStats.mouseSensitivity;

        statusEffectsController = new StatusEffectsController<MovementStatsNetwork, MovementStatsLocal>(currentMovementStats, baseMovementStats);
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        if (!_skinContoller.skills.disablingPlayerJumpAndGravity)
        {
            JumpAndGravity();
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (!_skinContoller.skills.disablingPlayerMove && !currentMovementStats.isStuned.Value)
        {
            Move();
        }

        //Debug.Log(currentMovementStats.moveSpeed.Value);
        
        GroundedCheck(); 
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        if (PauseScreen.isPause)
        {
            inputs.look = Vector2.zero;
        }

        CameraRotation();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z);
        grounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    /*void OnDrawGizmos()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        Gizmos.color = transparentRed;

        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z),
            groundedRadius);
    }*/

    private void CameraRotation()
    {
        if (inputs.look.sqrMagnitude >= _threshold)
        {
            float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            _cinemachineTargetYaw += inputs.look.x * deltaTimeMultiplier * currentMovementStats.mouseSensitivity.Value;
            _cinemachineTargetPitch += inputs.look.y * deltaTimeMultiplier * currentMovementStats.mouseSensitivity.Value;
        }

        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, bottomClamp, topClamp);

        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + cameraAngleOverride, _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        float targetSpeed;

        if (IsServer)
        {
            targetSpeed = currentMovementStats.moveSpeed.Value;
        }
        else
        {
            targetSpeed = currentMoveSpeed;
        }
        

        if (inputs.move == Vector2.zero)
        {
            targetSpeed = 0.0f;
        }

        float currentHorizontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
        //float speedOffset = 0.2f;

        _speed = targetSpeed;

        /*if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * speedChangeRate);

            speed = Mathf.Round(speed * 1000f) / 1000f;
        }
        else
        {
            speed = targetSpeed;
        }*/

        Vector3 inputDirection = new Vector3(inputs.move.x, 0.0f, inputs.move.y).normalized;

        _targetRotation = mainCamera.transform.eulerAngles.y;

        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

        Vector3 cameraForward = new Vector3(mainCamera.transform.forward.x, 0.0f, mainCamera.transform.forward.z).normalized;
        Vector3 cameraRight = new Vector3(mainCamera.transform.right.x, 0.0f, mainCamera.transform.right.z).normalized;

        _targetDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z).normalized;

        characterVelocity = _targetDirection * _speed * Time.deltaTime;

        //Debug.Log($"characterVelocity: {characterVelocity}, deltaTime: {Time.deltaTime}, targetDirection: {targetDirection}, speed: {speed}");

        controller.Move(characterVelocity + new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
    }

    private void JumpAndGravity()
    {
        if (grounded)
        {
            _fallTimeoutDelta = fallTimeout;

            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            if (inputs.jump && !currentMovementStats.isStuned.Value)
            {
                verticalVelocity = Mathf.Sqrt(currentMovementStats.jumpHeight.Value * -2f * gravity);
            }
        }
        else
        {
            if (!_skinContoller.skills.disablingPlayerMove)
            {
                inputs.jump = false;
            }

            if (verticalVelocity < _terminalVelocity)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }
    }

    public void ResetGravityEffect()
    {
        verticalVelocity = 0f;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);

    }

    /*public override void OnNetworkDespawn()
    {
        startCamera.SetActive(true);
    }*/
}
