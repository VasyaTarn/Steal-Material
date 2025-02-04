using Cinemachine;
using UnityEngine;
using Unity.Netcode;
using System.Linq;

public class PlayerSkillsController : NetworkBehaviour
{
    private SkinContoller _skin;
    private PlayerMovementController _playerMovementController;
    private Inputs _inputs;
    private PlayerObjectReferences _playerObjectReferences;

    public GameObject enemy;

    [Header("Aim")]
    [SerializeField] private CinemachineVirtualCamera _aimVirtualCamera;
    private RaycastPerformer _performer;
    private Vector3 _raycastPointPosition = Vector3.zero;

    [HideInInspector] public bool disablingPlayerShootingDuringMovementSkill = false;

    public PlayerHealthController enemyHealthController { get; private set; }
    public PlayerMovementController enemyMovementController { get; private set; }
    public PlayerSkillsController enemySkillsController { get; private set; }
    public PlayerObjectReferences enemyObjectReferences { get; private set; }

    //private Transform debugRay;

    private void Start()
    {
        if (!IsOwner)
            return;

        _skin = GetComponent<SkinContoller>();
        _inputs = GetComponent<Inputs>();
        _playerMovementController = GetComponent<PlayerMovementController>();
        _playerObjectReferences = GetComponent<PlayerObjectReferences>();
        _performer = GetComponent<RaycastPerformer>();

        /*if (IsServer)
        {
            debugRay = GameObject.Find("Debug ray").transform;
        }*/
    }

    private void FixedUpdate()
    {
        if (_skin != null)
        {
            if (_skin.skills is IFixedUpdateHandler fixedUpdateHandler)
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

        _skin.skills.Passive();

        if (!PauseScreen.isPause)
        {
            if (!_skin.disablingPlayerSkills)
            {
                if (_skin.skinMaterial != null && _inputs.meleeAttack && Time.time >= _skin.skills.lastMeleeAttackTime + _skin.skills.meleeAttackCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.MeleeAttack();
                    UIManager.Instance.Melee.ActivateCooldown(_skin.skills.meleeAttackCooldown);
                    _skin.skills.lastMeleeAttackTime = Time.time;
                }

                if (_inputs.aim)
                {
                    _aimVirtualCamera.gameObject.SetActive(true);
                }
                else
                {
                    _aimVirtualCamera.gameObject.SetActive(false);
                }

                if (IsOwner)
                {
                    if (_inputs.shoot && _playerObjectReferences.projectileSpawnPoint != null && Time.time >= _skin.skills.lastRangeAttackTime + _skin.skills.rangeAttackCooldown && !disablingPlayerShootingDuringMovementSkill && !_playerMovementController.currentMovementStats.isStuned.Value)
                    {
                        if (_performer.PerformRaycast(out RaycastHit raycastHit))
                        {
                            _skin.skills.RangeAttack(raycastHit);
                            _skin.skills.lastRangeAttackTime = Time.time;
                        }
                    }
                }

                if (_inputs.movementSkill && Time.time >= _skin.skills.lastMovementTime + _skin.skills.movementCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.Movement();
                    UIManager.Instance.Movement.ActivateCooldown(_skin.skills.movementCooldown);
                    _skin.skills.lastMovementTime = Time.time;
                }

                if (_inputs.defense && Time.time >= _skin.skills.lastDefenseTime + _skin.skills.defenseCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.Defense();
                    UIManager.Instance.Defense.ActivateCooldown(_skin.skills.defenseCooldown);
                    _skin.skills.lastDefenseTime = Time.time;
                }

                if (_inputs.special && Time.time >= _skin.skills.lastSpecialTime + _skin.skills.specialCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.Special();
                    UIManager.Instance.Special.ActivateCooldown(_skin.skills.specialCooldown);
                    _skin.skills.lastSpecialTime = Time.time;
                }
            }
        }

        if (_skin.skills is IUpdateHandler updateHandler)
        {
            updateHandler.HandleUpdate();
        }
    }

    public void SetEnemy(GameObject enemy)
    {
        this.enemy = enemy;
        enemyHealthController = enemy.GetComponent<PlayerHealthController>();
        enemyMovementController = enemy.GetComponent<PlayerMovementController>();
        enemySkillsController = enemy.GetComponent<PlayerSkillsController>();
        enemyObjectReferences = enemy.GetComponent<PlayerObjectReferences>();
    }
}
