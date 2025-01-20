using Unity.Netcode;
using UnityEngine;

public class Basic : MaterialSkills, ISkinMaterialChanger
{
    private GameObject _bulletPrefab;

    private int _initialProjctilePoolSize = 10;
    private LocalObjectPool _projectilePool;

    private SkinContoller _enemySkinController;

    private LayerMask _layerForSpecial;

    private bool _sprintStatus = false;
    private bool _inBlock = false;

    public override float meleeAttackCooldown { get; } = 1f;
    public override float rangeAttackCooldown { get; } = 1f;
    public override float movementCooldown { get; } = 0f;
    public override float defenseCooldown { get; } = 0f;
    public override float specialCooldown { get; } = 5f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Basic);

    private void Start()
    {
        _layerForSpecial = LayerMask.GetMask("Player");
        _bulletPrefab = projectilePrefabs[projectilePrefabKey];
    }

    #region Melee

    public override void MeleeAttack()
    {
        if (_inBlock)
        {
            DisableDefenseDuringOtherSkills();
        }

        Collider[] hitColliders = Physics.OverlapSphere(playerObjectReferences.basicMeleePointPosition.position, 0.5f, LayerMask.GetMask("Player"));

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
        if (_inBlock)
        {
            DisableDefenseDuringOtherSkills();
        }

        if (!IsServer)
        {
            SpawnProjectileLocal(raycastHit.point, playerObjectReferences.projectileSpawnPoint.position);
        }

        SpawnProjectileServerRpc(raycastHit.point, playerObjectReferences.projectileSpawnPoint.position, ownerId);
    }

    private void SpawnProjectileLocal(Vector3 raycastPoint, Vector3 projectileSpawnPoint)
    {
        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        if (_projectilePool == null)
        {
            if (_bulletPrefab != null)
            {
                _projectilePool = new LocalObjectPool(_bulletPrefab, _initialProjctilePoolSize);
            }
        }

        GameObject projectile = _projectilePool.Get(projectileSpawnPoint);

        projectile.GetComponent<BulletProjectile>().Movement(aimDir, () => _projectilePool.Release(projectile));
    }

    [Rpc(SendTo.Server)]
    private void SpawnProjectileServerRpc(Vector3 raycastPoint, Vector3 projectileSpawnPoint, ulong ownerId)
    {
        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(_bulletPrefab, projectileSpawnPoint);

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

            projectile.GetComponent<BulletProjectile>().Movement(aimDir, () =>
            {
                if (IsServer)
                {
                    if (projectile.IsSpawned)
                    {
                        if (characterController != null)
                        {
                            Physics.IgnoreCollision(projectileCollider, characterController, false);
                        }

                        projectile.Despawn();
                    }
                }
            });
        }
    }

    #endregion

    #region Movement

    public override void Movement()
    {
        if (playerMovementController.grounded)
        {
            if (_inBlock)
            {
                DisableDefenseDuringOtherSkills();
            }

            _sprintStatus = !_sprintStatus;

            if (IsServer)
            {
                if (_sprintStatus)
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
                if (_sprintStatus)
                {
                    playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed * 2;
                }
                else
                {
                    playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;
                }

                SwitchingSprintStatusRpc(_sprintStatus);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SwitchingSprintStatusRpc(bool sprintStatus)
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

    [Rpc(SendTo.Server)]
    private void DisableSprintRpc()
    {
        playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed;
    }

    #endregion

    #region Defense

    public override void Defense()
    {
        _sprintStatus = false;
        _inBlock = !_inBlock;

        if (_inBlock)
        {
            EnableDefense();

        }
        else
        {
            DisableDefense();
        }
    }

    private void EnableDefense()
    {
        if (IsServer)
        {
            playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed / 2;
        }
        else
        {
            playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed / 2;
            EnableDefenseRpc();
        }

        playerHealthController.EnableResistance(0.2f);
    }

    private void DisableDefense()
    {
        if (IsServer)
        {
            playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed;
        }
        else
        {
            playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;
            DisableDefenseRpc();
        }

        playerHealthController.DisableResistance();
    }

    [Rpc(SendTo.Server)]
    private void EnableDefenseRpc()
    {
        playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed / 2;
    }

    [Rpc(SendTo.Server)]
    private void DisableDefenseRpc()
    {
        playerSkillsController.enemyMovementController.currentMovementStats.moveSpeed.Value = playerSkillsController.enemyMovementController.baseMovementStats.moveSpeed;
    }

    private void DisableDefenseDuringOtherSkills()
    {
        _inBlock = false;
        DisableDefense();
    }

    #endregion

    #region Special

    public override void Special()
    {
        Collider[] hitColliders = Physics.OverlapSphere(Player.transform.position, 10f, _layerForSpecial);

        foreach (var hitCollider in hitColliders)
        {
            NetworkObject playerNetworkObject = hitCollider.gameObject.GetComponent<NetworkObject>();
            SkinContoller hitColliderSkin = hitCollider.gameObject.GetComponent<SkinContoller>();

            if (hitColliderSkin != null && playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                if (hitColliderSkin.skinMaterialNetworkVar.Value.TryGet(out NetworkObject networkObject))
                {
                    skinContoller.ChangeSkin(networkObject.gameObject);
                }
            }
        }
    }

    #endregion

    #region Passive

    public override void Passive()
    {
        if (playerSkillsController.enemy != null)
        {
            if (_enemySkinController == null)
            {
                _enemySkinController = playerSkillsController.enemy.GetComponent<SkinContoller>();
            }

            if (_enemySkinController.skinMaterialNetworkVar.Value.TryGet(out NetworkObject networkObject))
            {
                GameObject currentSkinMaterial = networkObject.gameObject;
                UIManager.Instance.GetEnemyMaterialDisplay().text = currentSkinMaterial.name;
            }
        }
    }

    #endregion

    public void ChangeSkinAction()
    {
        if (_sprintStatus)
        {
            _sprintStatus = false;

            if (IsServer)
            {
                playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed;
            }
            else
            {
                playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;

                DisableSprintRpc();
            }
        }
    }
}
