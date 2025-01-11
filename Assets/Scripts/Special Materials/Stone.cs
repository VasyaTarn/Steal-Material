using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Stone : MaterialSkills, ISkinMaterialChanger
{
    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.5f;
    public override float movementCooldown { get; } = 1f;
    public override float defenseCooldown { get; } = 0f;
    public override float specialCooldown { get; } = 5f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Stone);

    private GameObject bulletPrefab;
    
    private GameObject wall;

    private int initialProjctilePoolSize = 10;
    private LocalObjectPool projectilePool;

    private int initialWallPoolSize = 10;
    private LocalObjectPool wallPool;

    private bool canDash = true;
    private float dashingPower = 15f;
    private float dashingTime = 1.5f;
    private Vector3 dashVelocity;

    private void Start()
    {
        bulletPrefab = projectilePrefabs[projectilePrefabKey];
        wall = Resources.Load<GameObject>("Stone/Wall");
    }

    public override void meleeAttack()
    {
        if (Physics.Raycast(playerSkillsController.stoneMeleePointPosition.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            Collider[] hitColliders = Physics.OverlapSphere(hit.point, 1f, LayerMask.GetMask("Player"));

            foreach (Collider collider in hitColliders)
            {
                NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
                PlayerMovementController movementController = collider.gameObject.GetComponent<PlayerMovementController>();

                if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId && movementController != null)
                {
                    playerSkillsController.enemyHealthController.takeDamage(10);
                    if (IsServer)
                    {
                        movementController.statusEffectsController.addBuff(new Stun(5f));
                    }
                    else
                    {
                        applyStunRpc(playerNetworkObject.OwnerClientId);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void applyStunRpc(ulong ownerId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out NetworkClient networkClient))
        {
            networkClient.PlayerObject.gameObject.GetComponent<PlayerMovementController>().statusEffectsController.addBuff(new Stun(5f));
        }
    }

    public override void rangeAttack(RaycastHit raycastHit)
    {
        if (!IsServer)
        {
            spawnProjectileLocal(raycastHit.point, playerSkillsController.projectileSpawnPoint.position);
        }

        spawnProjectileServerRpc(raycastHit.point, playerSkillsController.projectileSpawnPoint.position, ownerId);
    }

    private void spawnProjectileLocal(Vector3 raycastPoint, Vector3 projectileSpawnPoint)
    {
        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        if (projectilePool == null)
        {
            if (bulletPrefab != null)
            {
                projectilePool = new LocalObjectPool(bulletPrefab, initialProjctilePoolSize);
            }
        }

        GameObject projectile = projectilePool.Get(projectileSpawnPoint);

        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, playerMovementController.controller, true);
        }

        projectile.GetComponent<BulletProjectile>().movement(aimDir, () =>
        {
            Physics.IgnoreCollision(projectileCollider, playerMovementController.controller, false);
            projectilePool.Release(projectile);
        });
    }

    [Rpc(SendTo.Server)]
    private void spawnProjectileServerRpc(Vector3 raycastPoint, Vector3 projectileSpawnPoint, ulong ownerId)
    {
        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(bulletPrefab, projectileSpawnPoint);

        projectile.Spawn();

        if (ownerId != 0)
        {
            projectile.NetworkHide(ownerId);
        }

        BulletProjectile bulletProjectile = projectile.GetComponent<BulletProjectile>();

        bulletProjectile.setOwnerId(ownerId);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out NetworkClient networkClient))
        {
            CharacterController characterController = networkClient.PlayerObject.gameObject.GetComponent<CharacterController>();
            Collider projectileCollider = projectile.GetComponent<Collider>();
            if (characterController != null && projectileCollider != null)
            {
                Physics.IgnoreCollision(projectileCollider, characterController, true);
            }

            projectile.GetComponent<BulletProjectile>().movement(aimDir, () =>
            {
                if (IsServer)
                {
                    if (projectile.IsSpawned)
                    {
                        Physics.IgnoreCollision(projectileCollider, characterController, false);
                        projectile.Despawn();
                    }
                }
            });
        }
    }

    public override void movement() 
    {
        if(canDash && playerMovementController.grounded)
        {
            StartCoroutine(dash());
        }
    }

    IEnumerator dash()
    {
        //Anim
        yield return new WaitForSeconds(0.2f);
        canDash = false;
        disablingPlayerMove = true;
        dashVelocity = playerMovementController.transform.forward * dashingPower;
        float startTime = Time.time;

        while (Time.time < startTime + dashingTime)
        {
            playerMovementController.controller.Move(dashVelocity * Time.deltaTime);
            yield return null;
        }

        disablingPlayerMove = false;
        canDash = true;
    }

    public override void defense()
    {
        if (Physics.Raycast(playerSkillsController.stoneDefensePointPosition.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            if (!IsServer)
            {
                spawnWallLocal(hit.point);
            }

            spawnWallServerRpc(hit.point, ownerId);
        }
    }

    private void spawnWallLocal(Vector3 wallSpawnPoint)
    {
        if (wallPool == null)
        {
            if (wall != null)
            {
                wallPool = new LocalObjectPool(wall, initialWallPoolSize);
            }
        }

        GameObject spawnedWall = wallPool.Get(wallSpawnPoint);

        Vector3 directionToPlayer = playerSkillsController.transform.position - wallSpawnPoint;

        directionToPlayer.y = 0;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            spawnedWall.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        StartCoroutine(releaseWall(() => wallPool.Release(spawnedWall)));
    }

    [Rpc(SendTo.Server)]
    private void spawnWallServerRpc(Vector3 wallSpawnPoint, ulong ownerId)
    {
        NetworkObject wallNetwork = NetworkObjectPool.Singleton.GetNetworkObject(wall, wallSpawnPoint);
        wallNetwork.Spawn();

        if (ownerId != 0)
        {
            wallNetwork.NetworkHide(ownerId);
        }

        Vector3 directionToPlayer;

        if (ownerId == 0)
        {
            directionToPlayer = playerSkillsController.transform.position - wallSpawnPoint;
        }
        else
        {
            directionToPlayer = playerSkillsController.enemy.transform.position - wallSpawnPoint;
        }

        directionToPlayer.y = 0;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            wallNetwork.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        StartCoroutine(releaseWall(() => 
        {
            if (IsServer)
            {
                if (wallNetwork.IsSpawned)
                {
                    wallNetwork.Despawn();
                }
            }
        }));
    }

    private IEnumerator releaseWall(Action releaseAction)
    {
        yield return new WaitForSeconds(5f);
        releaseAction?.Invoke();
    }

    public override void special()
    {
        
    }

    public override void passive()
    {
        enableDefense();
    }

    private void enableDefense()
    {
        if (IsServer)
        {
            playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed / 2;
        }
        else
        {
            playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed / 2;
            enableDefenseRpc();
        }

        playerHealthController.enableResistance(0.5f);
    }

    private void disableDefense()
    {
        if (IsServer)
        {
            playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed;
        }
        else
        {
            playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;
            disableDefenseRpc();
        }

        playerHealthController.disableResistance();
    }

    [Rpc(SendTo.Server)]
    private void enableDefenseRpc()
    {
        playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed / 2;
    }

    [Rpc(SendTo.Server)]
    private void disableDefenseRpc()
    {
        playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed;
    }

    public void ChangeSkinAction()
    {
        disableDefense();
    }
}

