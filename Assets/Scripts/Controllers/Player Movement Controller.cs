using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.Netcode;
using TMPro;
using System.Collections;
using Unity.VisualScripting;

public enum State
{
    Normal,
    Plant_Movement
}

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]

public class PlayerMovementController : NetworkBehaviour
{
    [Header("Player")]
    public MovementStats movementStats;

    //public float moveSpeed = 5.0f;
    [HideInInspector] public float currentMoveSpeed;

    [Range(0.0f, 0.3f)]
    public float rotationSmoothTime = 0.05f;

    public float speedChangeRate = 10.0f;

    [Space(20)]
    //public float jumpHeight = 1.2f;

    public float gravity = -15f;

    public float fallTimeout = 0.15f;

    [Space(20)]
    //public float mouseSensitivity = 2.0f;

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

    private float speed;
    private float targetRotation = 0.0f;
    private float rotationVelocity;
    public float verticalVelocity;
    private float terminalVelocity = 53.0f;
    [HideInInspector] public Vector3 characterVelocityMomentum;
    [HideInInspector] public Vector3 characterVelocity;
    [HideInInspector] public float momentumDrag = 3f;
    private Vector3 targetDirection;

    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    private float fallTimeoutDelta;

    private const float threshold = 0.01f;

    private PlayerInput playerInput;
    [HideInInspector] public CharacterController controller;
    [HideInInspector] public Inputs inputs;
    [HideInInspector] public GameObject mainCamera;
    private GameObject startCamera;
    private SkinContoller skinContoller;
    private BoxCollider boxCollider;

    //private bool rotateOnMove = true;
    //private bool isMovementDisabled = false;

    //public bool test = false;

    private bool isCurrentDeviceMouse
    {
        get
        {
            return playerInput.currentControlScheme == "KeyboardMouse";
        }
    }

    private void Awake()
    {
        Camera camera = GetComponentInChildren<Camera>();
        mainCamera = camera.gameObject;
    }

    private void Start()
    {
        if (!IsOwner)
        {
            mainCamera.SetActive(false);
            return;
        }

        startCamera = GameObject.Find("Start Camera");

        if(startCamera != null)
        {
            startCamera.SetActive(false);
        }

        Cursor.visible = false;

        Cursor.lockState = CursorLockMode.Locked;

        cinemachineTargetYaw = cinemachineCameraTarget.transform.rotation.eulerAngles.y;

        playerInput = GetComponent<PlayerInput>();
        controller = GetComponent<CharacterController>();
        boxCollider = GetComponent<BoxCollider>();
        inputs = GetComponent<Inputs>();
        skinContoller = GetComponent<SkinContoller>();

        if (controller != null && boxCollider != null)
        {
            Physics.IgnoreCollision(controller, boxCollider);
        }

        currentMoveSpeed = movementStats.moveSpeed;

        fallTimeoutDelta = fallTimeout;
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return; 

        //Debug.Log(characterVelocityMomentum);

        //ApplyMomentum();

        JumpAndGravity();
    }

    /*private void ApplyMomentum()
    {
        if (test)
        {
            characterVelocity += characterVelocityMomentum * Time.fixedDeltaTime;
        }

        if (characterVelocityMomentum.magnitude >= 0f)
        {
            characterVelocityMomentum -= characterVelocityMomentum * momentumDrag * Time.fixedDeltaTime;

            if (characterVelocityMomentum.magnitude < 0.01f)
            {
                test = false;
                characterVelocityMomentum = Vector3.zero;
            }
        }
    }*/


    private void Update()
    {
        if (!IsOwner)
            return;

        if (!skinContoller.skills.disablingPlayerMoveDuringMovementSkill)
        {
            Move();
        }

        GroundedCheck();
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

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
        if (inputs.look.sqrMagnitude >= threshold)
        {
            float deltaTimeMultiplier = isCurrentDeviceMouse ? 1.0f : Time.deltaTime;
            cinemachineTargetYaw += inputs.look.x * deltaTimeMultiplier * movementStats.mouseSensitivity;
            cinemachineTargetPitch += inputs.look.y * deltaTimeMultiplier * movementStats.mouseSensitivity;
        }

        cinemachineTargetYaw = ClampAngle(cinemachineTargetYaw, float.MinValue, float.MaxValue);
        cinemachineTargetPitch = ClampAngle(cinemachineTargetPitch, bottomClamp, topClamp);

        cinemachineCameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch + cameraAngleOverride, cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        float targetSpeed = currentMoveSpeed;

        if (inputs.move == Vector2.zero)
        {
            targetSpeed = 0.0f;
        }

        float currentHoriaontalSpeed = new Vector3(controller.velocity.x, 0.0f, controller.velocity.z).magnitude;
        float speedOffset = 0.1f;

        if (currentHoriaontalSpeed < targetSpeed - speedOffset || currentHoriaontalSpeed > targetSpeed + speedOffset)
        {
            speed = Mathf.Lerp(currentHoriaontalSpeed, targetSpeed, Time.deltaTime * speedChangeRate);

            speed = Mathf.Round(speed * 1000f) / 1000f;
        }
        else
        {
            speed = targetSpeed;
        }

        Vector3 inputDirection = new Vector3(inputs.move.x, 0.0f, inputs.move.y).normalized;

        targetRotation = mainCamera.transform.eulerAngles.y;

        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref rotationVelocity, rotationSmoothTime);

        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

        Vector3 cameraForward = new Vector3(mainCamera.transform.forward.x, 0.0f, mainCamera.transform.forward.z).normalized;
        Vector3 cameraRight = new Vector3(mainCamera.transform.right.x, 0.0f, mainCamera.transform.right.z).normalized;

        targetDirection = (cameraRight * inputDirection.x + cameraForward * inputDirection.z).normalized;

        characterVelocity = targetDirection * speed * Time.deltaTime;

        controller.Move(characterVelocity + new Vector3(0f, verticalVelocity, 0f) * Time.deltaTime);
        /*characterVelocity += characterVelocityMomentum;

        if (characterVelocityMomentum.magnitude >= 0f)
        {
            characterVelocityMomentum -= characterVelocityMomentum * momentumDrag * Time.deltaTime;
            if (characterVelocityMomentum.magnitude < 0.0f)
            {
                characterVelocityMomentum = Vector3.zero;
            }
        }*/
    }
     
    private void JumpAndGravity()
    {
        if (grounded)
        {
            fallTimeoutDelta = fallTimeout;

            if (verticalVelocity < 0.0f)
            {
                verticalVelocity = -2f;
            }

            if (inputs.jump)
            {
                verticalVelocity = Mathf.Sqrt(movementStats.jumpHeight * -2f * gravity);
            }
        }
        else
        {
            if (!skinContoller.skills.disablingPlayerMoveDuringMovementSkill)
            {
                inputs.jump = false;
            }

            if (verticalVelocity < terminalVelocity)
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }
    }

    public void resetGravityEffect()
    {
        verticalVelocity = 0f;
    }

    private static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360f) angle += 360f;
        if (angle > 360f) angle -= 360f;
        return Mathf.Clamp(angle, min, max);

    }
}
