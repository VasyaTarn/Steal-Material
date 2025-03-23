using System;
using System.Collections;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Plant : MaterialSkills, IUpdateHandler, ISkinMaterialChanger, IActivateSkinMaterialHandler
{
    private TrailRenderer _bulletTrail;
    private GameObject _summonedPlant;
    private GameObject _poisonExplosion;

    private int _initialProjctilePoolSize = 10;
    private LocalObjectPool _projectilePool;

    private int _initialSummonedEntityPoolSize = 10;
    private LocalObjectPool _summonedEntityPool;


    private int _initialPoisonExplosionPoolSize = 5;
    private LocalObjectPool _poisonExplosionPool;

    private bool _isRunningPassiveCoroutine = false;

    private float _retaliateEffectDuration = 4f;

    private bool _isRetaliateEffectActive = false;

    private Vector3 _hookshotPosition;
    private float _hookshotSpeed = 20f;
    private float _hookshotSize;
    private float _hookshotMaxLength = 70f;
    private float _hookshotSpeedMin = 20f;
    private float _hookshotSpeedMax = 40f;
    private float _hookshotSpeedMultiplier = 2f;
    private bool _isHookshotMoving = false;

    [SerializeField] private LayerMask _hookshotLayerMask;

    private float _bulletSpeed = 200f;

    private bool _throwing;
    //private bool jumpRequest = false;

    private float _climbSpeed = 3f;
    private bool _climbing = false;

    private bool _inJump = false;
    private bool _isJumpCoroutineRunning = false;

    /*private float climbJumpUpForce = 14f;
    private float climbJumpBackForce = 20f;*/

    private float _detectionLength = 0.42f;
    private float _sphereCastRadius = 0.1f;
    private float _maxWallLookAngle = 30f;
    private float _wallLookAngle;

    private RaycastHit _frontBotWallHit;
    private RaycastHit _frontTopWallHit;
    private bool _wallFrontBot;
    private bool _wallFrontTop;

    private Transform _lastWall;
    private Vector3 _lastWallNormal;
    private float _minWallNormalAngleChange = 5f;

    private bool _exitingWall;
    //private float exitWallTime = 1f;
    private float _exitWallTimer;

    private IDisposable _deathSubscription;

    public override float meleeAttackCooldown { get; } = 2f;
    public override float rangeAttackCooldown { get; } = 0.2f;
    public override float movementCooldown { get; } = 5f;
    public override float defenseCooldown { get; } = 7f;
    public override float specialCooldown { get; } = 3f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Plant);


    private void Start()
    {
        materialType = Type.Plant;

        //_bulletTrail = projectilePrefabs[projectilePrefabKey].GetComponent<TrailRenderer>();

        Addressables.LoadAssetAsync<GameObject>("Summon").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _summonedPlant = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Summon");
            }
        };

        Addressables.LoadAssetAsync<GameObject>("Poison_explosion_player").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _poisonExplosion = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Poison_explosion_player");
            }
        };

        //_summonedPlant = Resources.Load<GameObject>("Plant/Summon");
    }

    #region Melee

    public override void MeleeAttack()
    {
        if (!IsServer)
        {
            SpawnPoisonExplosionLocal(new Vector3(Player.transform.position.x, Player.transform.position.y - 0.99f, Player.transform.position.z));
        }

        SpawnPoisonExplosionServerRpc(new Vector3(Player.transform.position.x, Player.transform.position.y - 0.99f, Player.transform.position.z), ownerId);

        Collider[] hitColliders = Physics.OverlapSphere(Player.transform.position, 5f, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                playerSkillsController.enemyHealthController.TakeDamage(25f);
            }
        }
    }

    private void SpawnPoisonExplosionLocal(Vector3 poisonExplosionSpawnPoint)
    {
        if (_poisonExplosionPool == null)
        {
            if (_poisonExplosion != null)
            {
                _poisonExplosionPool = new LocalObjectPool(_poisonExplosion, _initialPoisonExplosionPoolSize);
            }
        }

        GameObject spawnedPoisonExplosion = _poisonExplosionPool.Get(poisonExplosionSpawnPoint);

        StartCoroutine(ReleasePoisonExplosion(3f, () => _poisonExplosionPool.Release(spawnedPoisonExplosion)));
    }

    [Rpc(SendTo.Server)]
    private void SpawnPoisonExplosionServerRpc(Vector3 poisonExplosionSpawnPoint, ulong ownerId)
    {
        NetworkObject poisonExplosionNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_poisonExplosion, poisonExplosionSpawnPoint);
        poisonExplosionNetwork.Spawn();

        if (ownerId != 0)
        {
            poisonExplosionNetwork.NetworkHide(ownerId);
        }

        StartCoroutine(ReleasePoisonExplosion(3f, () =>
        {
            if (IsServer)
            {
                if (poisonExplosionNetwork.IsSpawned)
                {
                    poisonExplosionNetwork.Despawn();
                }
            }
        }));
    }

    private IEnumerator ReleasePoisonExplosion(float duration, Action releaseAction)
    {
        yield return new WaitForSeconds(duration);

        releaseAction?.Invoke();
    }

    #endregion

    #region Range

    public override void RangeAttack(RaycastHit raycastHit)
    {
        if (!IsServer)
        {
            SpawnProjectileLocal(raycastHit.point, playerObjectReferences.ProjectileSpawnPoint.position);
        }

        SpawnProjectileServerRpc(raycastHit.point, playerObjectReferences.ProjectileSpawnPoint.position, ownerId);

        if (raycastHit.collider.gameObject.CompareTag("Player"))
        {
            playerSkillsController.enemyHealthController.TakeDamage(5f);
        }
    }

    private void SpawnProjectileLocal(Vector3 raycastPoint, Vector3 projectileSpawnPoint)
    {
        if (_bulletTrail == null)
        {
            _bulletTrail = projectilePrefabs[projectilePrefabKey].GetComponent<TrailRenderer>();
        }

        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        if (_projectilePool == null)
        {
            if (_bulletTrail != null)
            {
                _projectilePool = new LocalObjectPool(_bulletTrail.gameObject, _initialProjctilePoolSize);
            }
        }

        GameObject projectile = _projectilePool.Get(projectileSpawnPoint);

        StartCoroutine(TrailMovement(projectile, raycastPoint, () => _projectilePool.Release(projectile)));  
    }

    [Rpc(SendTo.Server)]
    private void SpawnProjectileServerRpc(Vector3 raycastPoint, Vector3 projectileSpawnPoint, ulong ownerId)
    {
        if (_bulletTrail == null)
        {
            _bulletTrail = projectilePrefabs[projectilePrefabKey].GetComponent<TrailRenderer>();
        }

        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(_bulletTrail.gameObject, projectileSpawnPoint);
        projectile.Spawn();

        if (ownerId != 0)
        {
            projectile.NetworkHide(ownerId);
        }

        StartCoroutine(TrailMovement(projectile, raycastPoint, () =>
        {
            if (IsServer)
            {
                if (projectile.IsSpawned)
                {
                    projectile.Despawn();
                }
            }
        }));
    }

    private IEnumerator TrailMovement(GameObject trail, Vector3 hitPoint, Action onComplete)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(trail.transform.position, hitPoint);
        float remainingDistance = distance;
        TrailRenderer trailRenderer = trail.GetComponent<TrailRenderer>();


        if (!trailRenderer.enabled)
        {
            trailRenderer.enabled = true;
        }


        trail.transform.LookAt(hitPoint);

        while (remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (remainingDistance / distance));

            remainingDistance -= _bulletSpeed * Time.deltaTime;

            yield return null;
        }

        trailRenderer.Clear();
        trailRenderer.enabled = false;

        trail.transform.position = hitPoint;

        onComplete?.Invoke();
    }

    private IEnumerator TrailMovement(NetworkObject trail, Vector3 hitPoint, Action onComplete)
    {
        Vector3 startPosition = trail.transform.position;
        float distance = Vector3.Distance(trail.transform.position, hitPoint);
        float remainingDistance = distance;
        TrailRenderer trailRenderer = trail.GetComponent<TrailRenderer>();

        if (!trailRenderer.enabled)
        {
            trailRenderer.enabled = true;
        }

        trail.transform.LookAt(hitPoint);

        while (remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (remainingDistance / distance));

            remainingDistance -= _bulletSpeed * Time.deltaTime;

            yield return null;
        }

        trailRenderer.Clear();
        trailRenderer.enabled = false;

        trail.transform.position = hitPoint;

        onComplete?.Invoke();
    }

    #endregion

    #region Movement

    public override void Movement()
    {
        if (!playerMovementController.disablingPlayerMove)
        {
            HandleHookshotStart();
        }
    }

    private void HandleHookshotStart()
    {
        if (Physics.Raycast(playerMovementController.mainCamera.transform.position, playerMovementController.mainCamera.transform.forward, out RaycastHit hit, Mathf.Infinity, _hookshotLayerMask))
        {
            playerSkillsController.SetDisablePlayerSkillsStatus(true);
            _hookshotPosition = hit.point;
            _hookshotSize = 0f;
            playerObjectReferences.HookshotTransform.gameObject.SetActive(true);
            playerObjectReferences.HookshotTransform.localScale = Vector3.zero;
            _throwing = true;
        }
    }

    private void HandleHookshotThrow()
    {
        playerObjectReferences.HookshotTransform.LookAt(_hookshotPosition);

        float hookshotThrowSpeed = 100f;
        _hookshotSize += hookshotThrowSpeed * Time.deltaTime;
        playerObjectReferences.HookshotTransform.localScale = new Vector3(0.02f, 0.02f, _hookshotSize);

        if(_hookshotSize > _hookshotMaxLength)
        {
            StopHookshot();
        }
        else
        {
            if (_hookshotSize >= Vector3.Distance(playerMovementController.transform.position, _hookshotPosition))
            {
                _throwing = false;
                playerMovementController.disablingPlayerMove = true;
                playerMovementController.disablingPlayerVerticalMove = true;
                Player.GetComponent<RaycastPerformer>().isActiveRaycast = false;
                _isHookshotMoving = true;
            }
        }
    }

    private Vector3 _hookshotDir;
    private void HandleHookshotMovement()
    {
        playerAnimationController.SetMovementSkillStatus(true);
        playerAnimationController.PlayBoolAnimation("IsFalling", true);

        _hookshotDir = (_hookshotPosition - playerMovementController.transform.position).normalized;

        _hookshotSpeed = Mathf.Clamp(Vector3.Distance(playerMovementController.transform.position, _hookshotPosition), _hookshotSpeedMin, _hookshotSpeedMax);

        playerMovementController.controller.Move(_hookshotDir * _hookshotSpeed * _hookshotSpeedMultiplier * Time.deltaTime);
        _hookshotSize -= _hookshotSpeed * _hookshotSpeedMultiplier * Time.deltaTime;

        if (_hookshotSize <= 0)
        {
            playerObjectReferences.HookshotTransform.localScale = Vector3.zero;
        }
        else
        {
            playerObjectReferences.HookshotTransform.localScale = new Vector3(0.02f, 0.02f, _hookshotSize);
        }

        float reachedHookshotPositionDistance = 2f;
        if (Vector3.Distance(playerMovementController.transform.position, _hookshotPosition) < reachedHookshotPositionDistance)
        {
            EndHookshot();
        }
    }

    private void EndHookshot()
    {
        playerMovementController.disablingPlayerMove = false;
        playerMovementController.disablingPlayerVerticalMove = false;
        playerSkillsController.SetDisablePlayerSkillsStatus(false);
        playerAnimationController.SetMovementSkillStatus(false);
        playerAnimationController.PlayBoolAnimation("IsFalling", false);
        Player.GetComponent<RaycastPerformer>().isActiveRaycast = true;
        _isHookshotMoving = false;
        playerMovementController.ResetGravityEffect();

        playerObjectReferences.HookshotTransform.gameObject.SetActive(false);
    }

    private void StopHookshot()
    {
        playerMovementController.disablingPlayerMove = false;
        playerMovementController.disablingPlayerVerticalMove = false;
        playerSkillsController.SetDisablePlayerSkillsStatus(false);
        playerAnimationController.SetMovementSkillStatus(false);
        playerAnimationController.PlayBoolAnimation("IsFalling", false);
        Player.GetComponent<RaycastPerformer>().isActiveRaycast = true;
        _isHookshotMoving = false;
        _throwing = false;

        playerObjectReferences.HookshotTransform.gameObject.SetActive(false);
    }

    #endregion

    #region Defense
    public override void Defense()
    {
        if(playerHealthController.OnDamageTaken == null)
        {
            playerHealthController.OnDamageTaken += HandleDamageTaken;
        }

        if (!_isRetaliateEffectActive)
        {
            StartCoroutine(ActivateRetaliateEffect());
        }
    }

    private IEnumerator ActivateRetaliateEffect()
    {
        _isRetaliateEffectActive = true;

        NetworkObject currentArmature = new();

        if (skinContoller.skinView.CurrentArmatureNetwork.Value.TryGet(out NetworkObject armatureNetworkObject))
        {
            currentArmature = armatureNetworkObject;
        }

        if (IsClient && !IsServer)
        {
            skinContoller.skinView.CurrentArmatureLocal.transform.Find("Spikes").gameObject.SetActive(true);
        }

        ChangeActiveStatusDefenseModelRpc(currentArmature, true);

        yield return new WaitForSeconds(_retaliateEffectDuration);

        _isRetaliateEffectActive = false;

        if (IsClient && !IsServer)
        {
            skinContoller.skinView.CurrentArmatureLocal.transform.Find("Spikes").gameObject.SetActive(false);
        }

        ChangeActiveStatusDefenseModelRpc(currentArmature, false);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ChangeActiveStatusDefenseModelRpc(NetworkObjectReference armature, bool status)
    {
        if (armature.TryGet(out NetworkObject armatureNetworkObject))
        {
            armatureNetworkObject.transform.Find("Spikes").gameObject.SetActive(status);
        }
    }

    private void HandleDamageTaken(float damage)
    {
        if (_isRetaliateEffectActive)
        {
            playerSkillsController.enemyHealthController.TakeDamageByRetaliate(damage + (damage * 0.1f));
        }
    }

    #endregion

    #region Special

    public override void Special() 
    {
        if (!IsServer)
        {
            SpawnSummonedEntityLocal(playerObjectReferences.SummonedEntitySpawnPoint.position);
        }

        SpawnSummonedEntityServerRpc(playerObjectReferences.SummonedEntitySpawnPoint.position, ownerId);

        playerHealthController.TakeDamage(50f);
    }

    private void SpawnSummonedEntityLocal(Vector3 summonedEntitySpawnPoint)
    {
        if (_summonedEntityPool == null)
        {
            if (_summonedPlant != null)
            {
                _summonedEntityPool = new LocalObjectPool(_summonedPlant, _initialSummonedEntityPoolSize);
            }
        }

        SummonedEntity summonedEntity = _summonedEntityPool.Get(summonedEntitySpawnPoint).GetComponent<SummonedEntity>();
        if(summonedEntity.owner == null)
        {
            summonedEntity.owner = playerSkillsController;
        }

        summonedEntity.SetDeathAction(() => _summonedEntityPool.Release(summonedEntity.gameObject));
    }

    [Rpc(SendTo.Server)]
    private void SpawnSummonedEntityServerRpc(Vector3 summonedEntitySpawnPoint, ulong ownerId)
    {
        NetworkObject summonedEntityNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_summonedPlant, summonedEntitySpawnPoint);
        summonedEntityNetwork.Spawn();

        if (ownerId != 0)
        {
            summonedEntityNetwork.NetworkHide(ownerId);
        }

        SummonedEntity summonedEntity = summonedEntityNetwork.GetComponent<SummonedEntity>();

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out NetworkClient client))
        {
            SetOwnerRpc(summonedEntityNetwork, client.PlayerObject);
            //summonedEntity.owner = client.PlayerObject.GetComponent<PlayerSkillsController>();
        }

        summonedEntity.SetDeathAction(() =>
        {
            if (IsServer)
            {
                if (summonedEntityNetwork.IsSpawned)
                {
                    summonedEntityNetwork.Despawn();
                }
            }
        });
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void SetOwnerRpc(NetworkObjectReference summon, NetworkObjectReference client)
    {
        if (summon.TryGet(out NetworkObject summonObject) && client.TryGet(out NetworkObject clientObject))
        {
            summonObject.GetComponent<SummonedEntity>().owner = clientObject.GetComponent<PlayerSkillsController>();
        }
    }

    #endregion

    #region Passive

    public override void Passive() 
    {
        if (playerHealthController.currentHp.Value < playerHealthController.healthStats.maxHp)
        {
            if (!_isRunningPassiveCoroutine) 
            {
                StartCoroutine(RegenerationCoroutine(1f));
            }
        }

        WallCheck();
        StateMachine();

        if (_climbing)
        {
            ClimbingMovement();
        }
    }

    private void WallCheck()
    {
        _wallFrontBot = Physics.SphereCast(new Vector3(playerMovementController.transform.position.x, playerMovementController.transform.position.y - 1f, playerMovementController.transform.position.z), _sphereCastRadius, playerMovementController.transform.forward, out _frontBotWallHit, _detectionLength + 0.5f, LayerMask.GetMask("Ground"));
        _wallFrontTop = Physics.SphereCast(new Vector3(playerMovementController.transform.position.x, playerMovementController.transform.position.y + 1f, playerMovementController.transform.position.z), _sphereCastRadius, playerMovementController.transform.forward, out _frontTopWallHit, _detectionLength, LayerMask.GetMask("Ground"));
        _wallLookAngle = Vector3.Angle(playerMovementController.transform.forward, -_frontBotWallHit.normal);

        bool newWall = _frontBotWallHit.transform != _lastWall || Mathf.Abs(Vector3.Angle(_lastWallNormal, _frontBotWallHit.normal)) > _minWallNormalAngleChange;
    }

    private void StateMachine()
    {
        if (!_wallFrontTop && !_wallFrontBot && _climbing)
        {
            StopClimbing();
            _inJump = true;
        }

        if (!_climbing && !_inJump)
        {
            if (_wallFrontTop && inputs.move.y == 1 && _wallLookAngle < _maxWallLookAngle && !_exitingWall)
            {
                StartClimbing();
            }
        }

        if(inputs.jump && _climbing)
        {
            StopClimbing();
            _inJump = true;
        }


        if(_inJump && !_isJumpCoroutineRunning)
        {
            StartCoroutine(test());
        }
    }

    private void StartClimbing()
    {
        _climbing = true;
        playerSkillsController.SetDisablePlayerSkillsStatus(true);
        Player.GetComponent<RaycastPerformer>().isActiveRaycast = false;
        playerAnimationController.SetClimbStatus(_climbing);
        playerMovementController.disablingPlayerMove = true;
        playerMovementController.disablingPlayerVerticalMove = true;

        _lastWall = _frontBotWallHit.transform;
        _lastWallNormal = _frontBotWallHit.normal;
    }

    private void ClimbingMovement()
    {
        playerMovementController.controller.Move(new Vector3(0f, _climbSpeed, 0f) * Time.deltaTime);
    }

    private void StopClimbing()
    {
        _climbing = false;
        playerSkillsController.SetDisablePlayerSkillsStatus(false);
        Player.GetComponent<RaycastPerformer>().isActiveRaycast = true;
        playerAnimationController.SetClimbStatus(_climbing);
        playerMovementController.disablingPlayerMove = false;
        playerMovementController.disablingPlayerVerticalMove = false;
        /*if (exitWallTimer <= 0)
        {
            disablingPlayerMoveDuringMovementSkill = false;
        }*/

        playerMovementController.ResetGravityEffect();
        playerMovementController.ExecuteJump(3f);
    }

    private IEnumerator test()
    {
        _isJumpCoroutineRunning = true;

        yield return new WaitForSeconds(0.5f);
        _inJump = false;

        _isJumpCoroutineRunning = false;
    }

    /*private Vector3 moveDirection = Vector3.zero;

    private void climbJump()
    {
        exitingWall = true;
        exitWallTimer = exitWallTime;

        Vector3 forceToApply = playerMovementController.transform.up * climbJumpUpForce + frontBotWallHit.normal * climbJumpBackForce;

        moveDirection = forceToApply;

        StartCoroutine(ApplyClimbJump());
    }

    private IEnumerator ApplyClimbJump()
    {
        disablingPlayerMoveDuringMovementSkill = true;

        while (*//*!playerMovementController.grounded*//*exitWallTimer > 0)
        {
            playerMovementController.controller.Move(moveDirection * Time.deltaTime);

            yield return null;
        }

        playerMovementController.characterVelocity = Vector3.zero;
        disablingPlayerMoveDuringMovementSkill = false;

        Debug.Log("Test");
        moveDirection = Vector3.zero;
    }*/

    private IEnumerator RegenerationCoroutine(float regenerationNumber)
    {
        _isRunningPassiveCoroutine = true;

        playerHealthController.Regeneration(regenerationNumber);
        yield return new WaitForSeconds(.1f);

        _isRunningPassiveCoroutine = false;
    }

    #endregion

    public void HandleUpdate()
    {
        if (_isHookshotMoving)
        {
            HandleHookshotMovement();
        }
        if (_throwing)
        {
            HandleHookshotThrow();
        }
    }

    public void ChangeSkinAction()
    {
        if (_isRetaliateEffectActive)
        {
            StopCoroutine(ActivateRetaliateEffect());
            _isRetaliateEffectActive = false;
        }

        if (_throwing)
        {
            StopHookshot();
        }
    }

    public void ActivateSkinMaterialAction()
    {
        if (_deathSubscription == null)
        {
            _deathSubscription = playerHealthController.OnDeath
                .Subscribe(HandlePlayerDeath);
        }
    }

    private void HandlePlayerDeath(ulong obj)
    {
        if (_isHookshotMoving)
        {
            _isHookshotMoving = false;
            playerObjectReferences.HookshotTransform.gameObject.SetActive(false);

            playerAnimationController.SetMovementSkillStatus(false);
            playerAnimationController.PlayBoolAnimation("IsFalling", false);

            Player.GetComponent<RaycastPerformer>().isActiveRaycast = true;

            playerMovementController.ResetGravityEffect();
            playerMovementController.disablingPlayerVerticalMove = false;
        }
    }
}
