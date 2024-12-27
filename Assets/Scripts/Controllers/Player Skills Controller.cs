using Cinemachine;
using UnityEngine;
using Unity.Netcode;
using Zenject;

public class PlayerSkillsController : NetworkBehaviour
{
    private SkinContoller skin;
    private PlayerMovementController playerMovementController;
    private Inputs inputs;
    private float lastMeleeAttackTime = 0.0f;
    private float lastRangeAttackTime = 0.0f;

    private GameObject enemy;
    private PlayerHealthController enemyHealthController;

    [Header("Aim")]
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private LayerMask aimCollaiderLayerMask;
    private Vector3 raycastPointPosition = Vector3.zero;

    [Header("Range Attack")]
    public Transform projectileSpawnPoint;

    [Header("Plant Objects")]
    public Transform hookshotTransform;
    public Transform summonedEntitySpawnPoint;

    //private Transform debugRay;

    private void Awake()
    {
        hookshotTransform.gameObject.SetActive(false);
    }

    private void Start()
    {
        if (!IsOwner)
            return;

        skin = GetComponent<SkinContoller>();
        inputs = GetComponent<Inputs>();
        playerMovementController = GetComponent<PlayerMovementController>();

        /*if (IsServer)
        {
            debugRay = GameObject.Find("Debug ray").transform;
        }*/
    }

    private void FixedUpdate()
    {
        if (skin != null)
        {
            if (skin.skills is IFixedUpdateHandler fixedUpdateHandler)
            {
                fixedUpdateHandler.HandleFixedUpdate();
            }
        }
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        /*if (performRaycast(out RaycastHit test))
        {
            debugRay.transform.position = test.point;
        }*/

        skin.skills.passive();

        if (skin.skinMaterial != null && inputs.meleeAttack && Time.time >= lastMeleeAttackTime + skin.skills.meleeAttackCooldown)
        {
            skin.skills.meleeAttack();
            lastMeleeAttackTime = Time.time;
        }

        /*Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        RaycastHit raycastHit;
        if (Physics.Raycast(ray, out raycastHit, Mathf.Infinity, aimCollaiderLayerMask))
        {
            
        }*/


        if (inputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
            //playerMovementController.setRotateOnMove(false);

            /*if(!skin.skills.disablingPlayerMoveDuringMovementSkill)
            {
                aimRotation();
            }*/
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
           // playerMovementController.setRotateOnMove(true);
        }

        /*if (inputs.shoot && projectileSpawnPoint != null && !skin.skills.disablingPlayerShootingDuringMovementSkill)
        {
            aimRotation();
            playerMovementController.setRotateOnMove(false);
        }*/
        /*else if (!inputs.shoot && !playerMovementController.getRotateOnMove() && !inputs.aim)
        {
            playerMovementController.setRotateOnMove(true);
        }*/

        if(IsOwner)
        {
            if (inputs.shoot && projectileSpawnPoint != null && Time.time >= lastRangeAttackTime + skin.skills.rangeAttackCooldown && !skin.skills.disablingPlayerShootingDuringMovementSkill)
            {
                if (performRaycast(out RaycastHit raycastHit))
                {
                    skin.skills.rangeAttack(raycastHit);
                    lastRangeAttackTime = Time.time;
                }
            }
        }

        if (inputs.movementSkill)
        {
            skin.skills.movement();
        }

        if(inputs.defense)
        {
            skin.skills.defense();
        }
        
        if(inputs.special)
        {
            skin.skills.special();
        }

        if (skin.skills is IUpdateHandler updateHandler)
        {
            updateHandler.HandleUpdate();
        }
    }

    /*private Vector3 worldAimTarget;
    private Vector3 aimDirection;

    private void aimRotation()
    {
        worldAimTarget = raycastPointPosition;
        worldAimTarget.y = transform.position.y;
        aimDirection = (worldAimTarget - transform.position).normalized;

        transform.forward = Vector3.Lerp(transform.forward, aimDirection, Time.deltaTime * 30f);
    }*/

    public void setEnemy(GameObject enemy)
    {
        this.enemy = enemy;
        enemyHealthController = enemy.GetComponent<PlayerHealthController>();
    }

    private bool performRaycast(out RaycastHit raycastHit)
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

        return Physics.Raycast(ray, out raycastHit, Mathf.Infinity, aimCollaiderLayerMask);
    }

    public GameObject getEnemy()
    {
        return enemy;
    }

    public PlayerHealthController getEnemyHealthController()
    {
        return enemyHealthController;
    }
}
