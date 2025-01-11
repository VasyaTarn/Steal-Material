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
    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.2f;
    public override float movementCooldown { get; } = 3f;
    public override float defenseCooldown { get; } = 10f;
    public override float specialCooldown { get; } = 1f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Plant);

    private TrailRenderer bulletTrail;
    private GameObject summonedPlant;

    private int initialProjctilePoolSize = 10;
    private LocalObjectPool projectilePool;

    private int initialSummonedEntityPoolSize = 10;
    private LocalObjectPool summonedEntityPool;

    private bool isRunningPassiveCoroutine = false;

    private float retaliateEffectDuration = 4f;
    //[HideInInspector] public float retaliateCooldownTime = 3f;

    private bool isRetaliateEffectActive = false;
    //private bool isRetaliateOnCooldown = false;

    private Vector3 hookshotPosition;
    private float hookshotSpeed = 20f;
    private float hookshotSize;
    private float hookshotSpeedMin = 20f;
    private float hookshotSpeedMax = 40f;
    private float hookshotSpeedMultiplier = 2f;
    private bool isHookshotMoving = false;

    private float bulletSpeed = 200f;

    private bool throwing;
    //private bool jumpRequest = false;

    private float climbSpeed = 5f;
    private bool climbing = false;

    /*private float climbJumpUpForce = 14f;
    private float climbJumpBackForce = 20f;*/

    private float detectionLength = 0.42f;
    private float sphereCastRadius = 0.1f;
    private float maxWallLookAngle = 30f;
    private float wallLookAngle;

    private RaycastHit frontBotWallHit;
    private RaycastHit frontTopWallHit;
    private bool wallFrontBot;
    private bool wallFrontTop;

    private Transform lastWall;
    private Vector3 lastWallNormal;
    private float minWallNormalAngleChange = 5f;

    private bool exitingWall;
    //private float exitWallTime = 1f;
    private float exitWallTimer;


    private void Start()
    {
        bulletTrail = projectilePrefabs[projectilePrefabKey].GetComponent<TrailRenderer>();
        summonedPlant = Resources.Load<GameObject>("Plant/Summon");
    }

    public override void meleeAttack()
    {
        Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, 5f, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                playerSkillsController.enemyHealthController.takeDamage(10);
            }
        }
    }

    public override void rangeAttack(RaycastHit raycastHit)
    {
        if (!IsServer)
        {
            spawnProjectileLocal(raycastHit.point, playerSkillsController.projectileSpawnPoint.position);
        }

        spawnProjectileServerRpc(raycastHit.point, playerSkillsController.projectileSpawnPoint.position, ownerId);

        if (raycastHit.collider.gameObject.CompareTag("Player"))
        {
            playerSkillsController.enemyHealthController.takeDamage(10f);
        }
    }

    private void spawnProjectileLocal(Vector3 raycastPoint, Vector3 projectileSpawnPoint)
    {
        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        if (projectilePool == null)
        {
            if (bulletTrail != null)
            {
                projectilePool = new LocalObjectPool(bulletTrail.gameObject, initialProjctilePoolSize);
            }
        }

        GameObject projectile = projectilePool.Get(projectileSpawnPoint);


        StartCoroutine(trailMovement(projectile, raycastPoint, () => projectilePool.Release(projectile)));  
    }

    [Rpc(SendTo.Server)]
    private void spawnProjectileServerRpc(Vector3 raycastPoint, Vector3 projectileSpawnPoint, ulong ownerId)
    {
        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(bulletTrail.gameObject, projectileSpawnPoint);
        projectile.Spawn();

        if (ownerId != 0)
        {
            projectile.NetworkHide(ownerId);
        }

        StartCoroutine(trailMovement(projectile, raycastPoint, () =>
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

    private IEnumerator trailMovement(GameObject trail, Vector3 hitPoint, Action onComplete)
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

            remainingDistance -= bulletSpeed * Time.deltaTime;

            yield return null;
        }

        trailRenderer.enabled = false;

        trail.transform.position = hitPoint;

        onComplete?.Invoke();
    }

    private IEnumerator trailMovement(NetworkObject trail, Vector3 hitPoint, Action onComplete)
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

            remainingDistance -= bulletSpeed * Time.deltaTime;

            yield return null;
        }

        trailRenderer.enabled = false;

        trail.transform.position = hitPoint;

        onComplete?.Invoke();
    }

    public override void movement()
    {
        if (!disablingPlayerMove)
        {
            handleHookshotStart();
        }
    }

    public override void defense()
    {
        if(playerHealthController.OnDamageTaken == null)
        {
            playerHealthController.OnDamageTaken += HandleDamageTaken;
        }

        if (!isRetaliateEffectActive)
        {
            StartCoroutine(ActivateRetaliateEffect());
        }
    }

    public override void special() 
    {
        if (!IsServer)
        {
            spawnSummonedEntityLocal(playerSkillsController.summonedEntitySpawnPoint.position);
        }

        spawnSummonedEntityServerRpc(playerSkillsController.summonedEntitySpawnPoint.position, ownerId);
    }

    private void spawnSummonedEntityLocal(Vector3 summonedEntitySpawnPoint)
    {
        if (summonedEntityPool == null)
        {
            if (summonedPlant != null)
            {
                summonedEntityPool = new LocalObjectPool(summonedPlant, initialSummonedEntityPoolSize);
            }
        }

        SummonedEntity summonedEntity = summonedEntityPool.Get(summonedEntitySpawnPoint).GetComponent<SummonedEntity>();
        if(summonedEntity.owner == null)
        {
            summonedEntity.owner = playerSkillsController;
        }

        summonedEntity.setDeathAction(() => summonedEntityPool.Release(summonedEntity.gameObject));
    }

    [Rpc(SendTo.Server)]
    private void spawnSummonedEntityServerRpc(Vector3 summonedEntitySpawnPoint, ulong ownerId)
    {
        NetworkObject summonedEntityNetwork = NetworkObjectPool.Singleton.GetNetworkObject(summonedPlant, summonedEntitySpawnPoint);
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

        summonedEntity.setDeathAction(() =>
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

    public override void passive() 
    {
        if (playerHealthController.currentHp.Value < playerHealthController.healthStats.maxHp)
        {
            if (!isRunningPassiveCoroutine) 
            {
                StartCoroutine(regenerationCoroutine(1f));
            }
        }

        wallCheck();
        stateMachine();

        if (climbing)
        {
            climbingMovement();
        }
    }

    private void wallCheck()
    {
        wallFrontBot = Physics.SphereCast(new Vector3(playerMovementController.transform.position.x, playerMovementController.transform.position.y - 1f, playerMovementController.transform.position.z), sphereCastRadius, playerMovementController.transform.forward, out frontBotWallHit, detectionLength, LayerMask.GetMask("Ground"));
        wallFrontTop = Physics.SphereCast(new Vector3(playerMovementController.transform.position.x, playerMovementController.transform.position.y + 1f, playerMovementController.transform.position.z), sphereCastRadius, playerMovementController.transform.forward, out frontTopWallHit, detectionLength, LayerMask.GetMask("Ground"));
        wallLookAngle = Vector3.Angle(playerMovementController.transform.forward, -frontBotWallHit.normal);

        bool newWall = frontBotWallHit.transform != lastWall || Mathf.Abs(Vector3.Angle(lastWallNormal, frontBotWallHit.normal)) > minWallNormalAngleChange;
    }

    private void stateMachine()
    {
        if (wallFrontTop && inputs.move.y == 1 && wallLookAngle < maxWallLookAngle && !exitingWall)
        {
            if (!climbing)
            {
                startClimbing();
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
            if ((climbing && !wallFrontBot) || (climbing && inputs.move.y < 1))
            {
                stopClimbing();
            }
        }

    }

    private void startClimbing()
    {
        climbing = true;
        disablingPlayerMove = true;

        lastWall = frontBotWallHit.transform;
        lastWallNormal = frontBotWallHit.normal;
    }

    private void climbingMovement()
    {
        playerMovementController.controller.Move(new Vector3(0f, climbSpeed, 0f) * Time.deltaTime);
    }

    private void stopClimbing()
    {
        climbing = false;
        disablingPlayerMove = false;
        /*if (exitWallTimer <= 0)
        {
            disablingPlayerMoveDuringMovementSkill = false;
        }*/

        playerMovementController.resetGravityEffect();
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

    private IEnumerator ActivateRetaliateEffect()
    {
        isRetaliateEffectActive = true;

        yield return new WaitForSeconds(retaliateEffectDuration);

        isRetaliateEffectActive = false;

        /*StartCoroutine(RetaliateCooldown());*/
    }

    /*private IEnumerator RetaliateCooldown()
    {
        isRetaliateOnCooldown = true;

        yield return new WaitForSeconds(retaliateCooldownTime);

        isRetaliateOnCooldown = false;
    }*/

    private void HandleDamageTaken(float damage)
    {
        if (isRetaliateEffectActive)
        {
            playerSkillsController.enemyHealthController.takeDamageByRetaliate(damage + (damage * 0.1f));
        }
    }

    private IEnumerator regenerationCoroutine(float regenerationNumber)
    {
        isRunningPassiveCoroutine = true;

        playerHealthController.regeneration(regenerationNumber);
        yield return new WaitForSeconds(.1f);

        isRunningPassiveCoroutine = false;
    }

    private void handleHookshotStart()
    {
        if (Physics.Raycast(playerMovementController.mainCamera.transform.position, playerMovementController.mainCamera.transform.forward, out RaycastHit hit))
        {
            hookshotPosition = hit.point;
            hookshotSize = 0f;
            playerSkillsController.hookshotTransform.gameObject.SetActive(true);
            playerSkillsController.hookshotTransform.localScale = Vector3.zero;
            throwing = true;
        }
    }

    private void handleHookshotThrow()
    {
        playerSkillsController.hookshotTransform.LookAt(hookshotPosition);

        float hookshotThrowSpeed = 100f;
        hookshotSize += hookshotThrowSpeed * Time.deltaTime;
        playerSkillsController.hookshotTransform.localScale = new Vector3(1, 1, hookshotSize);

        if(hookshotSize >= Vector3.Distance(playerMovementController.transform.position, hookshotPosition))
        {
            throwing = false;
            disablingPlayerMove = true;
            isHookshotMoving = true;
        }
    }

    private Vector3 hookshotDir;
    private void handleHookshotMovement()
    {
        hookshotDir = (hookshotPosition - playerMovementController.transform.position).normalized;

        hookshotSpeed = Mathf.Clamp(Vector3.Distance(playerMovementController.transform.position, hookshotPosition), hookshotSpeedMin, hookshotSpeedMax);

        playerMovementController.controller.Move(hookshotDir * hookshotSpeed * hookshotSpeedMultiplier * Time.deltaTime);
        hookshotSize -= hookshotSpeed * hookshotSpeedMultiplier * Time.deltaTime;

        if (hookshotSize <= 0)
        {
            playerSkillsController.hookshotTransform.localScale = Vector3.zero;
        }
        else
        {
            playerSkillsController.hookshotTransform.localScale = new Vector3(1, 1, hookshotSize);
        }

        float reachedHookshotPositionDistance = 2f;
        if(Vector3.Distance(playerMovementController.transform.position, hookshotPosition) < reachedHookshotPositionDistance)
        {
            stopHookshot();
        }
    }

    private void stopHookshot()
    {
        disablingPlayerMove = false;
        isHookshotMoving = false;
        playerMovementController.resetGravityEffect();

        playerSkillsController.hookshotTransform.gameObject.SetActive(false);
    }

    /*private void handleJump()
    {
        float speedMultiplier = 0.1f;
        playerMovementController.characterVelocityMomentum = hookshotDir * speedMultiplier;

        float jumpMultiplier = 0.3f;
        playerMovementController.characterVelocityMomentum += Vector3.up * jumpMultiplier;

        stopHookshot();
    }
*/
    public void HandleUpdate()
    {
        if (isHookshotMoving)
        {
            handleHookshotMovement();
        }
        if (throwing)
        {
            handleHookshotThrow();
        }
    }

    public void ChangeSkinAction()
    {
        if (isRetaliateEffectActive)
        {
            Debug.Log("Test");
            StopCoroutine(ActivateRetaliateEffect());
            isRetaliateEffectActive = false;
        }

        if (throwing)
        {
            stopHookshot();
        }
    }
}
