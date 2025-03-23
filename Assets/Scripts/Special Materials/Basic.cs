using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Basic : MaterialSkills, ISkinMaterialChanger, IUpdateHandler
{
    private GameObject _bulletPrefab;

    private int _initialProjctilePoolSize = 10;
    private LocalObjectPool _projectilePool;

    private GameObject _claw;

    private int _initialClawPoolSize = 1;
    private LocalObjectPool _clawPool;

    private SkinContoller _enemySkinController;

    private LayerMask _layerForSpecial;

    private bool _sprintStatus = false;
    private bool _inBlock = false;

    private float _specialRaduis = 10f;

    public override float meleeAttackCooldown { get; } = 1f;
    public override float rangeAttackCooldown { get; } = 0.5f;
    public override float movementCooldown { get; } = 0f;
    public override float defenseCooldown { get; } = 0f;
    public override float specialCooldown { get; } = 5f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Basic);


    private void Start()
    {
        materialType = Type.Basic;

        _layerForSpecial = LayerMask.GetMask("Player");

        //_bulletPrefab = projectilePrefabs[projectilePrefabKey];

        Addressables.LoadAssetAsync<GameObject>("ClawAttack").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _claw = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load ClawAttack");
            }
        };

        //_claw = Resources.Load<GameObject>("Basic/ClawAttack");
    }

    #region Melee

    public override void MeleeAttack()
    {
        if (_inBlock)
        {
            DisableDefenseDuringOtherSkills();
        }

        playerAnimationController.PlayTriggerAnimation("BasicMeleeAttack");

        StartCoroutine(PerformAttack());
    }

    private IEnumerator PerformAttack()
    {
        yield return new WaitForSeconds(0.2f);

        if (!IsServer)
        {
            SpawnClawLocal(playerObjectReferences.BasicMeleePointPosition.position);
        }

        SpawnClawServerRpc(playerObjectReferences.BasicMeleePointPosition.position, ownerId);

        Collider[] hitColliders = Physics.OverlapSphere(playerObjectReferences.BasicMeleePointPosition.position, 0.5f, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();

            if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {

                playerSkillsController.enemyHealthController.TakeDamage(20);
            }
        }
    }

    private void SpawnClawLocal(Vector3 clawSpawnPoint)
    {
        if (_clawPool == null)
        {
            if (_claw != null)
            {
                _clawPool = new LocalObjectPool(_claw, _initialClawPoolSize);
            }
        }

        GameObject spawnedClaw = _clawPool.Get(clawSpawnPoint);

        spawnedClaw.transform.rotation = Quaternion.Euler(40f, 0f, -90f);

        Vector3 directionToPlayer = Player.transform.position - clawSpawnPoint;

        directionToPlayer.y = 0;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            float targetYRotation = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;

            Quaternion finalRotation = Quaternion.Euler(90f, targetYRotation, 90f);

            spawnedClaw.transform.rotation = finalRotation;
        }

        StartCoroutine(ReleaseStaticObject(0.5f, () => _clawPool.Release(spawnedClaw)));
    }

    [Rpc(SendTo.Server)]
    private void SpawnClawServerRpc(Vector3 clawNetworkSpawnPoint, ulong ownerId)
    {
        NetworkObject clawNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_claw, clawNetworkSpawnPoint);
        clawNetwork.Spawn();

        if (ownerId != 0)
        {
            clawNetwork.NetworkHide(ownerId);
        }

        clawNetwork.transform.rotation = Quaternion.Euler(40f, 0f, -90f);

        Vector3 directionToPlayer = NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject.transform.position - clawNetworkSpawnPoint;

        directionToPlayer.y = 0;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            float targetYRotation = Mathf.Atan2(directionToPlayer.x, directionToPlayer.z) * Mathf.Rad2Deg;

            Quaternion finalRotation = Quaternion.Euler(90f, targetYRotation, 90f);

            clawNetwork.transform.rotation = finalRotation;
        }

        StartCoroutine(ReleaseStaticObject(0.5f, () =>
        {
            if (IsServer)
            {
                if (clawNetwork.IsSpawned)
                {
                    clawNetwork.Despawn();
                }
            }
        }));
    }

    private IEnumerator ReleaseStaticObject(float duration, Action releaseAction)
    {
        yield return new WaitForSeconds(duration);
        releaseAction?.Invoke();
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
            SpawnProjectileLocal(raycastHit.point, playerObjectReferences.ProjectileSpawnPoint.position);
        }

        SpawnProjectileServerRpc(raycastHit.point, playerObjectReferences.ProjectileSpawnPoint.position, ownerId);
    }

    private void SpawnProjectileLocal(Vector3 raycastPoint, Vector3 projectileSpawnPoint)
    {
        if (_bulletPrefab == null)
        {
            _bulletPrefab = projectilePrefabs[projectilePrefabKey];
        }

        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        if (_projectilePool == null)
        {
            if (_bulletPrefab != null)
            {
                _projectilePool = new LocalObjectPool(_bulletPrefab, _initialProjctilePoolSize);
            }
        }

        GameObject projectile = _projectilePool.Get(projectileSpawnPoint);

        if (projectile != null && projectile.transform.childCount > 0)
        {
            for (int i = 0; i < projectile.transform.childCount; i++)
            {
                if (projectile.transform.GetChild(i).TryGetComponent(out TrailRenderer trailRenderer))
                {
                    if (!trailRenderer.enabled)
                    {
                        trailRenderer.enabled = true;
                    }
                }
            }
        }

        projectile.GetComponent<BulletProjectile>().Movement(aimDir, () =>
        {
            if (projectile != null && projectile.transform.childCount > 0)
            {
                for (int i = 0; i < projectile.transform.childCount; i++)
                {
                    if (projectile.transform.GetChild(i).TryGetComponent(out TrailRenderer trailRenderer))
                    {
                        trailRenderer.Clear();
                        trailRenderer.enabled = false;
                    }

                    if (projectile.transform.GetChild(i).TryGetComponent(out VisualEffect visualEffect))
                    {
                        visualEffect.Stop();
                    }
                }
            }

            StartCoroutine(DelayRelease(projectile, 0.2f));
        });
    }

    [Rpc(SendTo.Server)]
    private void SpawnProjectileServerRpc(Vector3 raycastPoint, Vector3 projectileSpawnPoint, ulong ownerId)
    {
        if (_bulletPrefab == null)
        {
            _bulletPrefab = projectilePrefabs[projectilePrefabKey];
        }

        Vector3 aimDir = (raycastPoint - projectileSpawnPoint).normalized;

        NetworkObject projectile = NetworkObjectPool.Singleton.GetNetworkObject(_bulletPrefab, projectileSpawnPoint);

        projectile.Spawn();

        if (ownerId != 0)
        {
            projectile.NetworkHide(ownerId);
        }

        if (projectile != null && projectile.transform.childCount > 0)
        {
            for (int i = 0; i < projectile.transform.childCount; i++)
            {
                if (projectile.transform.GetChild(i).TryGetComponent(out TrailRenderer trailRenderer))
                {
                    if (!trailRenderer.enabled)
                    {
                        trailRenderer.enabled = true;
                    }
                }
            }
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

                        if (projectile != null && projectile.transform.childCount > 0)
                        {
                            for (int i = 0; i < projectile.transform.childCount; i++)
                            {
                                if (projectile.transform.GetChild(i).TryGetComponent(out TrailRenderer trailRenderer))
                                {
                                    trailRenderer.Clear();
                                    trailRenderer.enabled = false;
                                }

                                if(projectile.transform.GetChild(i).TryGetComponent(out VisualEffect visualEffect))
                                {
                                    visualEffect.Stop();
                                }
                            }
                        }

                        StartCoroutine(DelayDespawn(projectile, 0.2f));
                    }
                }
            });
        }
    }

    private IEnumerator DelayRelease(GameObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);
        _projectilePool.Release(projectile);
    }

    private IEnumerator DelayDespawn(NetworkObject projectile, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (projectile.IsSpawned)
        {
            projectile.Despawn();
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

            playerAnimationController.SetSprintStatus(_sprintStatus);

            if (IsServer)
            {
                if (_sprintStatus)
                {
                    playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed * 1.5f;
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
                    playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed * 1.5f;
                }
                else
                {
                    playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;
                }

                SwitchingSprintStatusRpc(_sprintStatus, ownerId);
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void SwitchingSprintStatusRpc(bool sprintStatus, ulong ownerId)
    {
        PlayerMovementController player = NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject.GetComponent<PlayerMovementController>();

        if (sprintStatus)
        {
            player.currentMovementStats.moveSpeed.Value = player.baseMovementStats.moveSpeed * 1.5f;
        }
        else
        {
            player.currentMovementStats.moveSpeed.Value = player.baseMovementStats.moveSpeed;
        }
    }

    [Rpc(SendTo.Server)]
    private void DisableSprintRpc(ulong ownerId)
    {
        PlayerMovementController player = NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject.GetComponent<PlayerMovementController>();

        player.currentMovementStats.moveSpeed.Value = player.baseMovementStats.moveSpeed;
    }

    #endregion

    #region Defense

    public override void Defense()
    {
        _sprintStatus = false;
        playerAnimationController.SetSprintStatus(_sprintStatus);

        _inBlock = !_inBlock;
        playerAnimationController.SetBlockStatus(_inBlock);

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
        playerAnimationController.SetBlockStatus(_inBlock);

        DisableDefense();
    }

    #endregion

    #region Special

    public override void Special()
    {
        Collider[] hitColliders = Physics.OverlapSphere(Player.transform.position, _specialRaduis, _layerForSpecial);

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
                UIReferencesManager.Instance.EnemyMaterialDisplay.text = "Enemy:\n" + currentSkinMaterial.name;
            }
        }
    }

    #endregion

    public void ChangeSkinAction()
    {
        if (_sprintStatus)
        {
            _sprintStatus = false;

            playerAnimationController.SetSprintStatus(_sprintStatus);

            if (IsServer)
            {
                playerMovementController.currentMovementStats.moveSpeed.Value = playerMovementController.baseMovementStats.moveSpeed;
            }
            else
            {
                playerMovementController.currentMoveSpeed = playerMovementController.baseMovementStats.moveSpeed;

                DisableSprintRpc(ownerId);
            }
        }

        UIReferencesManager.Instance.EnemyMaterialDisplay.text = "";

        if (playerObjectReferences.BasicSkillRadius.activeSelf)
        {
            playerObjectReferences.BasicSkillRadius.SetActive(false);
        }
    }

    public void HandleUpdate()
    {
        if(!playerObjectReferences.BasicSkillRadius.activeSelf)
        {
            float visualSkillRaduis = _specialRaduis * 2f;
            playerObjectReferences.BasicSkillRadius.transform.localScale = new Vector3(visualSkillRaduis, visualSkillRaduis, visualSkillRaduis);

            playerObjectReferences.BasicSkillRadius.SetActive(true);
        }
    }
}
