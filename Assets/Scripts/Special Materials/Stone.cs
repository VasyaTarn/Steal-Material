using DG.Tweening;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Stone : MaterialSkills, ISkinMaterialChanger
{
    private GameObject _bulletPrefab;
    
    private GameObject _wall;

    private GameObject _smoke;

    private GameObject _stoneburst;

    private GameObject _wallSmoke;

    private int _initialProjctilePoolSize = 10;
    private LocalObjectPool _projectilePool;

    private int _initialWallPoolSize = 1;
    private LocalObjectPool _wallPool;

    private int _initialSmokePoolSize = 4;
    private LocalObjectPool _smokePool;

    private int _initialStoneburstPoolSize = 3;
    private LocalObjectPool _stoneburstPool;

    private bool _canDash = true;
    private float _dashingPower = 15f;
    private float _dashingTime = 1.5f;
    private Vector3 _dashVelocity;

    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.5f;
    public override float movementCooldown { get; } = 4f;
    public override float defenseCooldown { get; } = 8f;
    public override float specialCooldown { get; } = 5f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Stone);


    private void Start()
    {
        materialType = Type.Stone;

        //_bulletPrefab = projectilePrefabs[projectilePrefabKey];

        Addressables.LoadAssetAsync<GameObject>("Wall").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _wall = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Wall");
            }
        };

        Addressables.LoadAssetAsync<GameObject>("Smoke").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _smoke = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Smoke");
            }
        };

        Addressables.LoadAssetAsync<GameObject>("Stoneburst").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _stoneburst = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Stoneburst");
            }
        };

        Addressables.LoadAssetAsync<GameObject>("Wall_smoke").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _wallSmoke = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Wall_smoke");
            }
        };

        /*_wall = Resources.Load<GameObject>("Stone/Wall");
        _smoke = Resources.Load<GameObject>("Stone/Smoke");
        _stoneburst = Resources.Load<GameObject>("Stone/Stoneburst");
        _wallSmoke = Resources.Load<GameObject>("Stone/Wall_smoke");*/
    }

    #region Melee

    public override void MeleeAttack()
    {
        if (Physics.Raycast(playerObjectReferences.StoneMeleePointPosition.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            if (!IsServer)
            {
                SpawnStoneburstLocal(hit.point);
            }

            SpawnStoneburstServerRpc(hit.point, ownerId);

            Collider[] hitColliders = Physics.OverlapSphere(hit.point, 1f, LayerMask.GetMask("Player"));

            foreach (Collider collider in hitColliders)
            {
                NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
                PlayerMovementController movementController = collider.gameObject.GetComponent<PlayerMovementController>();

                if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId && movementController != null)
                {
                    playerSkillsController.enemyHealthController.TakeDamage(10);
                    if (IsServer)
                    {
                        movementController.statusEffectsController.AddBuff(new Stun(5f));
                    }
                    else
                    {
                        ApplyStunRpc(playerNetworkObject.OwnerClientId);
                    }
                }
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void ApplyStunRpc(ulong ownerId)
    {
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out NetworkClient networkClient))
        {
            networkClient.PlayerObject.gameObject.GetComponent<PlayerMovementController>().statusEffectsController.AddBuff(new Stun(5f));
        }
    }

    private void SpawnStoneburstLocal(Vector3 stoneburstSpawnPoint)
    {
        if (_stoneburstPool == null)
        {
            if (_stoneburst != null)
            {
                _stoneburstPool = new LocalObjectPool(_stoneburst, _initialStoneburstPoolSize);
            }
        }

        GameObject spawnedStoneburst = _stoneburstPool.Get(stoneburstSpawnPoint);

        StartCoroutine(ReleaseStaticObject(3.5f, () => _stoneburstPool.Release(spawnedStoneburst)));
    }

    [Rpc(SendTo.Server)]
    private void SpawnStoneburstServerRpc(Vector3 stoneburstNetworkSpawnPoint, ulong ownerId)
    {
        NetworkObject stoneburstNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_stoneburst, stoneburstNetworkSpawnPoint);
        stoneburstNetwork.Spawn();

        if (ownerId != 0)
        {
            stoneburstNetwork.NetworkHide(ownerId);
        }

        StartCoroutine(ReleaseStaticObject(3.5f, () =>
        {
            if (IsServer)
            {
                if (stoneburstNetwork.IsSpawned)
                {
                    stoneburstNetwork.Despawn();
                }
            }
        }));
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

        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, playerMovementController.controller, true);
        }

        projectile.GetComponent<BulletProjectile>().Movement(aimDir, () =>
        {
            Physics.IgnoreCollision(projectileCollider, playerMovementController.controller, false);
            _projectilePool.Release(projectile);
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

        BulletProjectile bulletProjectile = projectile.GetComponent<BulletProjectile>();

        bulletProjectile.SetOwnerId(ownerId);

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
        if(_canDash && playerMovementController.grounded)
        {
            StartCoroutine(Dash());
        }
    }

    IEnumerator Dash()
    {
        //Anim
        yield return new WaitForSeconds(0.2f);
        _canDash = false;
        playerMovementController.disablingPlayerMove = true;
        _dashVelocity = playerMovementController.transform.forward * _dashingPower;
        float startTime = Time.time;

        while (Time.time < startTime + _dashingTime)
        {
            playerMovementController.controller.Move(_dashVelocity * Time.deltaTime);
            yield return null;
        }

        playerMovementController.disablingPlayerMove = false;
        _canDash = true;
    }

    #endregion

    #region Defense

    public override void Defense()
    {
        if (Physics.Raycast(playerObjectReferences.StoneDefensePointPosition.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
        {
            if (!IsServer)
            {
                SpawnWallLocal(hit.point);
            }

            SpawnWallServerRpc(hit.point, ownerId, player.GetComponent<NetworkObject>());
        }
    }

    private void SpawnWallLocal(Vector3 wallSpawnPoint)
    {
        if (_wallPool == null)
        {
            if (_wall != null)
            {
                _wallPool = new LocalObjectPool(_wall, _initialWallPoolSize);
            }
        }

        GameObject spawnedWall = _wallPool.Get(wallSpawnPoint);

        GameObject smoke = Instantiate(_wallSmoke, wallSpawnPoint, Quaternion.identity);

        Vector3 directionToPlayer = playerSkillsController.transform.position - wallSpawnPoint;

        directionToPlayer.y = 0;

        if (directionToPlayer.sqrMagnitude > 0.001f)
        {
            spawnedWall.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            smoke.transform.rotation = Quaternion.LookRotation(directionToPlayer);
        }

        spawnedWall.transform.position -= new Vector3(0, spawnedWall.transform.localScale.y, 0);
        float pos = (spawnedWall.transform.position + new Vector3(0, spawnedWall.transform.localScale.y, 0)).y;
        spawnedWall.transform.DOMoveY(pos, 1f).SetEase(Ease.InOutQuint);

        StartCoroutine(ReleaseStaticObject(5f, () =>
        {
            _wallPool.Release(spawnedWall);
            Destroy(smoke);
        }, () =>
        {
            float pos = (spawnedWall.transform.position - new Vector3(0, spawnedWall.transform.localScale.y, 0)).y;

            spawnedWall.transform.DOMoveY(pos, 1f).SetEase(Ease.InOutQuint);
        }));
    }

    [Rpc(SendTo.Server)]
    private void SpawnWallServerRpc(Vector3 wallSpawnPoint, ulong ownerId, NetworkObjectReference playerObjectReference)
    {
        NetworkObject wallNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_wall, wallSpawnPoint);

        NetworkObject smoke = Instantiate(_wallSmoke, wallSpawnPoint, Quaternion.identity).GetComponent<NetworkObject>();

        wallNetwork.Spawn();
        smoke.Spawn();

        if (ownerId != 0)
        {
            wallNetwork.NetworkHide(ownerId);
            smoke.NetworkHide(ownerId);
        }

        Vector3 directionToPlayer;

        if (playerObjectReference.TryGet(out NetworkObject playerObject))
        {
            directionToPlayer = playerObject.transform.position - wallSpawnPoint;

            directionToPlayer.y = 0;

            if (directionToPlayer.sqrMagnitude > 0.001f)
            {
                wallNetwork.transform.rotation = Quaternion.LookRotation(directionToPlayer);
                smoke.transform.rotation = Quaternion.LookRotation(directionToPlayer);
            }

            wallNetwork.transform.position -= new Vector3(0, wallNetwork.transform.localScale.y, 0);

            float pos = (wallNetwork.transform.position + new Vector3(0, wallNetwork.transform.localScale.y, 0)).y;


            wallNetwork.transform.DOMoveY(pos, 1f).SetEase(Ease.InOutQuint);

            StartCoroutine(ReleaseStaticObject(5f, () =>
            {
                if (IsServer)
                {
                    if (wallNetwork.IsSpawned)
                    {
                        wallNetwork.Despawn();
                        smoke.Despawn();
                    }
                }
            }, () =>
            {
                float pos = (wallNetwork.transform.position - new Vector3(0, wallNetwork.transform.localScale.y, 0)).y;

                wallNetwork.transform.DOMoveY(pos, 1f).SetEase(Ease.InOutQuint);
            }));
        }

        /*if (ownerId == 0)
        {
            Debug.Log("Server");
            directionToPlayer = playerSkillsController.transform.position - wallSpawnPoint;
        }
        else
        {
            Debug.Log("Client");
            directionToPlayer = playerSkillsController.enemy.transform.position - wallSpawnPoint;
        }*/
    }

    #endregion

    #region Special

    public override void Special()
    {
        foreach(Transform smokePosition in playerObjectReferences.StoneSpecialSmokePositions)
        {
            if (Physics.Raycast(smokePosition.position, Vector3.down, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Ground")))
            {
                if (!IsServer)
                {
                    SpawnSmokeLocal(hit.point);
                }

                SpawnSmokeServerRpc(hit.point, ownerId);
            }
        }
    }

    private void SpawnSmokeLocal(Vector3 smokeSpawnPoint)
    {
        if (_smokePool == null)
        {
            if (_smoke != null)
            {
                _smokePool = new LocalObjectPool(_smoke, _initialSmokePoolSize);
            }
        }

        GameObject spawnedSmoke = _smokePool.Get(smokeSpawnPoint);

        StartCoroutine(ReleaseStaticObject(5f, () => _smokePool.Release(spawnedSmoke), () =>
        {
            spawnedSmoke.GetComponent<VisualEffect>().Stop();
        }));
    }

    [Rpc(SendTo.Server)]
    private void SpawnSmokeServerRpc(Vector3 smokeSpawnPoint, ulong ownerId)
    {
        NetworkObject smokeNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_smoke, smokeSpawnPoint);
        smokeNetwork.Spawn();

        if (ownerId != 0)
        {
            smokeNetwork.NetworkHide(ownerId);
        }

        StartCoroutine(ReleaseStaticObject(5f, () =>
        {
            if (IsServer)
            {
                if (smokeNetwork.IsSpawned)
                {
                    smokeNetwork.Despawn();
                }
            }
        }, () =>
        {
            smokeNetwork.GetComponent<VisualEffect>().Stop();
        }));
    }

    #endregion

    #region Passive

    public override void Passive()
    {
        EnableDefense();
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
            EnableDefenseRpc(ownerId);
        }

        playerHealthController.EnableResistance(0.5f);
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
            DisableDefenseRpc(ownerId);
        }

        playerHealthController.DisableResistance();
    }

    [Rpc(SendTo.Server)]
    private void EnableDefenseRpc(ulong ownerId)
    {
        PlayerMovementController player = NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject.GetComponent<PlayerMovementController>();
        player.currentMovementStats.moveSpeed.Value = player.baseMovementStats.moveSpeed / 2;
    }

    [Rpc(SendTo.Server)]
    private void DisableDefenseRpc(ulong ownerId)
    {
        PlayerMovementController player = NetworkManager.Singleton.ConnectedClients[ownerId].PlayerObject.GetComponent<PlayerMovementController>();
        player.currentMovementStats.moveSpeed.Value = player.baseMovementStats.moveSpeed;
    }

    #endregion

    private IEnumerator ReleaseStaticObject(float duration, Action releaseAction)
    {
        yield return new WaitForSeconds(duration);
        releaseAction?.Invoke();
    }

    private IEnumerator ReleaseStaticObject(float duration, Action releaseAction, Action animation)
    {
        yield return new WaitForSeconds(duration - 1f);
        animation?.Invoke();
        yield return new WaitForSeconds(1f);
        releaseAction?.Invoke();
    }

    public void ChangeSkinAction()
    {
        DisableDefense();
    }
}

