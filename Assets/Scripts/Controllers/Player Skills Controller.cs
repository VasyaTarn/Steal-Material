using Cinemachine;
using UnityEngine;
using Unity.Netcode;
using System.Linq;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Unity.VisualScripting;

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

    private DamageIndicator _damageIndicator;

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

        _damageIndicator = new();
        _damageIndicator.Initialize(gameObject);

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

        _damageIndicator.RotateToTarget();

        _skin.skills.Passive();

        if (!PauseScreen.isPause)
        {
            if (!_skin.disablingPlayerSkills)
            {
                if (_skin.skinMaterial != null && _inputs.meleeAttack && Time.time >= _skin.skills.lastMeleeAttackTime + _skin.skills.meleeAttackCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.MeleeAttack();
                    UIReferencesManager.Instance.Melee.ActivateCooldown(_skin.skills.meleeAttackCooldown);
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
                    if (_inputs.shoot && _playerObjectReferences.ProjectileSpawnPoint != null && Time.time >= _skin.skills.lastRangeAttackTime + _skin.skills.rangeAttackCooldown && !disablingPlayerShootingDuringMovementSkill && !_playerMovementController.currentMovementStats.isStuned.Value)
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
                    UIReferencesManager.Instance.Movement.ActivateCooldown(_skin.skills.movementCooldown);
                    _skin.skills.lastMovementTime = Time.time;
                    
                }

                if (_inputs.defense && Time.time >= _skin.skills.lastDefenseTime + _skin.skills.defenseCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.Defense();
                    UIReferencesManager.Instance.Defense.ActivateCooldown(_skin.skills.defenseCooldown);
                    _skin.skills.lastDefenseTime = Time.time;
                }

                if (_inputs.special && Time.time >= _skin.skills.lastSpecialTime + _skin.skills.specialCooldown && !_playerMovementController.currentMovementStats.isStuned.Value)
                {
                    _skin.skills.Special();
                    UIReferencesManager.Instance.Special.ActivateCooldown(_skin.skills.specialCooldown);
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

    public void SetDisablePlayerSkillsStatus(bool status)
    {
        _skin.disablingPlayerSkills = status;
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner)
        {
            _damageIndicator.Cleanup();
        }
    }
}

public class DamageIndicator
{
    private PlayerHealthController _healthController;
    private PlayerSkillsController _skillsController;

    private CanvasGroup _canvasGroup;
    private RectTransform _rectTransform;

    private Quaternion tRot = Quaternion.identity;
    private Vector3 tPos = Vector3.zero;

    public void Initialize(GameObject player)
    {
        _canvasGroup = UIReferencesManager.Instance.DamageIndicator;
        _rectTransform = _canvasGroup.GetComponent<RectTransform>();
        _healthController = player.GetComponent<PlayerHealthController>();
        _skillsController = player.GetComponent<PlayerSkillsController>();

        _canvasGroup.alpha = 0f;

        _healthController.OnDamageTaken += HandleDamageTaken;
    }

    public void RotateToTarget()
    {
        if (_skillsController.enemy != null)
        {
            tPos = _skillsController.enemy.transform.position;
            tRot = _skillsController.enemy.transform.rotation;
        }

        Vector3 direction = _skillsController.transform.position - tPos;

        tRot = Quaternion.LookRotation(direction);
        tRot.z = -tRot.y;
        tRot.x = 0;
        tRot.y = 0;

        Vector3 northDirection = new Vector3(0, 0, _skillsController.transform.eulerAngles.y);
        _rectTransform.localRotation = tRot * Quaternion.Euler(northDirection);
    }

    private void HandleDamageTaken(float obj)
    {
        ShowIndicator().Forget();
    }

    private async UniTask ShowIndicator()
    {
        _canvasGroup.DOFade(1, 0.5f);

        await UniTask.Delay(2000);

        _canvasGroup.DOFade(0, 0.5f);

        await UniTask.Delay(500);

        _canvasGroup.DOKill();
    }

    public void Cleanup()
    {
        _healthController.OnDamageTaken -= HandleDamageTaken;
        Debug.Log("Cleanup");
    }
}
