using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class Plant : MaterialSkills, IUpdateHandler, ISkinMaterialChanger
{
    private TrailRenderer _bulletTrail;
    private GameObject _summonedPlant;

    private int _initialProjctilePoolSize = 10;
    private LocalObjectPool _projectilePool;

    private int _initialSummonedEntityPoolSize = 10;
    private LocalObjectPool _summonedEntityPool;

    private bool _isRunningPassiveCoroutine = false;

    private float _retaliateEffectDuration = 4f;

    private bool _isRetaliateEffectActive = false;

    private Vector3 _hookshotPosition;
    private float _hookshotSpeed = 20f;
    private float _hookshotSize;
    private float _hookshotSpeedMin = 20f;
    private float _hookshotSpeedMax = 40f;
    private float _hookshotSpeedMultiplier = 2f;
    private bool _isHookshotMoving = false;

    private float _bulletSpeed = 200f;

    private bool _throwing;
    //private bool jumpRequest = false;

    private float _climbSpeed = 5f;
    private bool _climbing = false;

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

    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.2f;
    public override float movementCooldown { get; } = 3f;
    public override float defenseCooldown { get; } = 10f;
    public override float specialCooldown { get; } = 1f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Plant);


    private void Start()
    {
        _bulletTrail = projectilePrefabs[projectilePrefabKey].GetComponent<TrailRenderer>();
        _summonedPlant = Resources.Load<GameObject>("Plant/Summon");
    }

    #region Melee

    public override void MeleeAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(Player.transform.position, 5f, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                playerSkillsController.enemyHealthController.TakeDamage(10);
            }
        }
    }

    #endregion

    #region Range

    public override void RangeAttack(RaycastHit raycastHit)
    {
        if (!IsServer)
        {
            SpawnProjectileLocal(raycastHit.point, playerObjectReferences.projectileSpawnPoint.position);
        }

        SpawnProjectileServerRpc(raycastHit.point, playerObjectReferences.projectileSpawnPoint.position, ownerId);

        if (raycastHit.collider.gameObject.CompareTag("Player"))
        {
            playerSkillsController.enemyHealthController.TakeDamage(10f);
        }
    }

    private void SpawnProjectileLocal(Vector3 raycastPoint, Vector3 projectileSpawnPoint)
    {
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

        while (remainingDistance > 0)
        {
            trail.transform.position = Vector3.Lerp(startPosition, hitPoint, 1 - (remainingDistance / distance));

            remainingDistance -= _bulletSpeed * Time.deltaTime;

            yield return null;
        }

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
        if (!disablingPlayerMove)
        {
            HandleHookshotStart();
        }
    }

    private void HandleHookshotStart()
    {
        if (Physics.Raycast(playerMovementController.mainCamera.transform.position, playerMovementController.mainCamera.transform.forward, out RaycastHit hit))
        {
            _hookshotPosition = hit.point;
            _hookshotSize = 0f;
            playerObjectReferences.hookshotTransform.gameObject.SetActive(true);
            playerObjectReferences.hookshotTransform.localScale = Vector3.zero;
            _throwing = true;
        }
    }

    private void HandleHookshotThrow()
    {
        playerObjectReferences.hookshotTransform.LookAt(_hookshotPosition);

        float hookshotThrowSpeed = 100f;
        _hookshotSize += hookshotThrowSpeed * Time.deltaTime;
        playerObjectReferences.hookshotTransform.localScale = new Vector3(1, 1, _hookshotSize);

        if (_hookshotSize >= Vector3.Distance(playerMovementController.transform.position, _hookshotPosition))
        {
            _throwing = false;
            disablingPlayerMove = true;
            _isHookshotMoving = true;
        }
    }

    private Vector3 _hookshotDir;
    private void HandleHookshotMovement()
    {
        _hookshotDir = (_hookshotPosition - playerMovementController.transform.position).normalized;

        _hookshotSpeed = Mathf.Clamp(Vector3.Distance(playerMovementController.transform.position, _hookshotPosition), _hookshotSpeedMin, _hookshotSpeedMax);

        playerMovementController.controller.Move(_hookshotDir * _hookshotSpeed * _hookshotSpeedMultiplier * Time.deltaTime);
        _hookshotSize -= _hookshotSpeed * _hookshotSpeedMultiplier * Time.deltaTime;

        if (_hookshotSize <= 0)
        {
            playerObjectReferences.hookshotTransform.localScale = Vector3.zero;
        }
        else
        {
            playerObjectReferences.hookshotTransform.localScale = new Vector3(1, 1, _hookshotSize);
        }

        float reachedHookshotPositionDistance = 2f;
        if (Vector3.Distance(playerMovementController.transform.position, _hookshotPosition) < reachedHookshotPositionDistance)
        {
            StopHookshot();
        }
    }

    private void StopHookshot()
    {
        disablingPlayerMove = false;
        _isHookshotMoving = false;
        playerMovementController.ResetGravityEffect();

        playerObjectReferences.hookshotTransform.gameObject.SetActive(false);
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

        yield return new WaitForSeconds(_retaliateEffectDuration);

        _isRetaliateEffectActive = false;
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
            SpawnSummonedEntityLocal(playerObjectReferences.summonedEntitySpawnPoint.position);
        }

        SpawnSummonedEntityServerRpc(playerObjectReferences.summonedEntitySpawnPoint.position, ownerId);
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
            summonedEntity.owner = client.PlayerObject.GetComponent<PlayerSkillsController>();
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
        _wallFrontBot = Physics.SphereCast(new Vector3(playerMovementController.transform.position.x, playerMovementController.transform.position.y - 1f, playerMovementController.transform.position.z), _sphereCastRadius, playerMovementController.transform.forward, out _frontBotWallHit, _detectionLength, LayerMask.GetMask("Ground"));
        _wallFrontTop = Physics.SphereCast(new Vector3(playerMovementController.transform.position.x, playerMovementController.transform.position.y + 1f, playerMovementController.transform.position.z), _sphereCastRadius, playerMovementController.transform.forward, out _frontTopWallHit, _detectionLength, LayerMask.GetMask("Ground"));
        _wallLookAngle = Vector3.Angle(playerMovementController.transform.forward, -_frontBotWallHit.normal);

        bool newWall = _frontBotWallHit.transform != _lastWall || Mathf.Abs(Vector3.Angle(_lastWallNormal, _frontBotWallHit.normal)) > _minWallNormalAngleChange;
    }

    private void StateMachine()
    {
        if (_wallFrontTop && inputs.move.y == 1 && _wallLookAngle < _maxWallLookAngle && !_exitingWall)
        {
            if (!_climbing)
            {
                StartClimbing();
            }
        }
        /*else if (exitingWall)
        {
            if (climbing) stopClimbing();


            if (exitWallTimer > 0) exitWallTimer -= Time.deltaTime;
            if (exitWallTimer < 0) exitingWall = false;
        }*/
        else
        {
            if ((_climbing && !_wallFrontBot) || (_climbing && inputs.move.y < 1))
            {
                StopClimbing();
            }
        }

    }

    private void StartClimbing()
    {
        _climbing = true;
        disablingPlayerMove = true;

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
        disablingPlayerMove = false;
        /*if (exitWallTimer <= 0)
        {
            disablingPlayerMoveDuringMovementSkill = false;
        }*/

        playerMovementController.ResetGravityEffect();
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
}
