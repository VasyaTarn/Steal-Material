using Unity.Netcode;
using UnityEngine;

public class Basic : MaterialSkills, ISkinMaterialChanger
{
    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.5f;
    public override float movementCooldown { get; } = 0f;
    public override float defenseCooldown { get; } = 0f;
    public override float specialCooldown { get; } = 5f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Basic);

    private GameObject bulletPrefab;

    private int initialProjctilePoolSize = 10;
    private LocalObjectPool projectilePool;

    private SkinContoller enemySkinController;

    private LayerMask layerForSpecial;

    private bool sprintStatus = false;
    private bool inBlock = false;


    private void Start()
    {
        layerForSpecial = LayerMask.GetMask("Player");
        bulletPrefab = projectilePrefabs[projectilePrefabKey];
    }

    public override void meleeAttack()
    {
        if (inBlock)
        {
            disableDefenseDuringOtherSkills();
        }

        Collider[] hitColliders = Physics.OverlapSphere(playerSkillsController.basicMeleePointPosition.position, 0.5f, LayerMask.GetMask("Player"));

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
        if (inBlock)
        {
            disableDefenseDuringOtherSkills();
        }

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

        projectile.GetComponent<BulletProjectile>().movement(aimDir, () => projectilePool.Release(projectile));
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
        if (playerMovementController.grounded)
        {
            if (inBlock)
            {
                disableDefenseDuringOtherSkills();
            }

            sprintStatus = !sprintStatus;

            if (IsServer)
            {
                if (sprintStatus)
                {
                    playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed * 2;
                }
                else
                {
                    playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed;
                }
            }
            else
            {
                if (sprintStatus)
                {
                    playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed * 2;
                }
                else
                {
                    playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;
                }

                switchingSprintStatusRpc(sprintStatus);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void switchingSprintStatusRpc(bool sprintStatus)
    {
        if (sprintStatus)
        {
            playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed * 2;
        }
        else
        {
            playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed;
        }
    }

    public override void defense()
    {
        sprintStatus = false;
        inBlock = !inBlock;

        if (inBlock)
        {
            enableDefense();

        }
        else
        {
            disableDefense();
        }
    }

    public override void special()
    {
        Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, 10f, layerForSpecial);

        foreach (var hitCollider in hitColliders)
        {
            NetworkObject playerNetworkObject = hitCollider.gameObject.GetComponent<NetworkObject>();
            SkinContoller hitColliderSkin = hitCollider.gameObject.GetComponent<SkinContoller>();

            if (hitColliderSkin != null && playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                if (hitColliderSkin.skinMaterialNetworkVar.Value.TryGet(out NetworkObject networkObject))
                {
                    skinContoller.changeSkin(networkObject.gameObject);
                }
            }
        }
    }

    public override void passive()
    {
        if (playerSkillsController.enemy != null)
        {
            if (enemySkinController == null)
            {
                enemySkinController = playerSkillsController.enemy.GetComponent<SkinContoller>();
            }

            if (enemySkinController.skinMaterialNetworkVar.Value.TryGet(out NetworkObject networkObject))
            {
                GameObject currentSkinMaterial = networkObject.gameObject;
                UIManager.Instance.getEnemyMaterialDisplay().text = currentSkinMaterial.name;
            }
        }
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

        playerHealthController.enableResistance(0.2f);
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

    private void disableDefenseDuringOtherSkills()
    {
        inBlock = false;
        disableDefense();
    }

    public void ChangeSkinAction()
    {
        if (sprintStatus)
        {
            sprintStatus = false;

            if (IsServer)
            {
                playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed;
            }
            else
            {
                playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;

                disableSprintRpc();
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void disableSprintRpc()
    {
        playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed;
    }
}
