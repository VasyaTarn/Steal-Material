using Cinemachine;
using UnityEngine;
using Unity.Netcode;

public class PlayerSkillsController : NetworkBehaviour
{
    private SkinContoller skin;
    private PlayerMovementController playerMovementController;
    private Inputs inputs;
    /*private float lastMeleeAttackTime = 0.0f;
    private float lastRangeAttackTime = 0.0f;
    private float lastMovementTime = 0.0f;*/
    public GameObject enemy { get; private set; }
    public PlayerHealthController enemyHealthController { get; private set; }
    public PlayerMovementController enemyMovementController { get; private set; }
    public PlayerSkillsController enemySkillsController { get; private set; }

    [Header("Aim")]
    [SerializeField] private CinemachineVirtualCamera aimVirtualCamera;
    [SerializeField] private LayerMask aimCollaiderLayerMask;
    private Vector3 raycastPointPosition = Vector3.zero;

    [Header("Range Attack")]
    public Transform projectileSpawnPoint;

    [Header("Player Objects")]
    public GameObject model;

    [Header("Plant Objects")]
    public Transform hookshotTransform;
    public Transform summonedEntitySpawnPoint;

    [Header("Basic Objects")]
    public Transform basicMeleePointPosition;
    //private Transform debugRay;

    [Header("Stone Objects")]
    public Transform stoneMeleePointPosition;
    public Transform stoneDefensePointPosition;

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

        if (skin.skinMaterial != null && inputs.meleeAttack && Time.time >= skin.skills.lastMeleeAttackTime + skin.skills.meleeAttackCooldown && !playerMovementController.currentMovementStats.isStuned.Value)
        {
            skin.skills.meleeAttack();
            skin.skills.lastMeleeAttackTime = Time.time;
        }


        if (inputs.aim)
        {
            aimVirtualCamera.gameObject.SetActive(true);
        }
        else
        {
            aimVirtualCamera.gameObject.SetActive(false);
        }

        if(IsOwner)
        {
            if (inputs.shoot && projectileSpawnPoint != null && Time.time >= skin.skills.lastRangeAttackTime + skin.skills.rangeAttackCooldown && !skin.skills.disablingPlayerShootingDuringMovementSkill && !playerMovementController.currentMovementStats.isStuned.Value)
            {
                if (performRaycast(out RaycastHit raycastHit))
                {
                    skin.skills.rangeAttack(raycastHit);
                    skin.skills.lastRangeAttackTime = Time.time;
                }
            }
        }

        if (inputs.movementSkill && Time.time >= skin.skills.lastMovementTime + skin.skills.movementCooldown && !playerMovementController.currentMovementStats.isStuned.Value)
        {
            skin.skills.movement();
            skin.skills.lastMovementTime = Time.time;
        }

        if(inputs.defense && Time.time >= skin.skills.lastDefenseTime + skin.skills.defenseCooldown && !playerMovementController.currentMovementStats.isStuned.Value)
        {
            skin.skills.defense();
            skin.skills.lastDefenseTime = Time.time;
        }
        
        if(inputs.special && Time.time >= skin.skills.lastSpecialTime + skin.skills.specialCooldown && !playerMovementController.currentMovementStats.isStuned.Value)
        {
            skin.skills.special();
            skin.skills.lastSpecialTime = Time.time;
        }

        if (skin.skills is IUpdateHandler updateHandler)
        {
            updateHandler.HandleUpdate();
        }
    }

    public void setEnemy(GameObject enemy)
    {
        this.enemy = enemy;
        enemyHealthController = enemy.GetComponent<PlayerHealthController>();
        enemyMovementController = enemy.GetComponent<PlayerMovementController>();
        enemySkillsController = enemy.GetComponent<PlayerSkillsController>();
    }

    private bool performRaycast(out RaycastHit raycastHit)
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);

        return Physics.Raycast(ray, out raycastHit, Mathf.Infinity, aimCollaiderLayerMask);
    }
}
