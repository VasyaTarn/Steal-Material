using System.Collections;
using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Unity.VisualScripting;

public class Fire : MaterialSkills, ISkinMaterialChanger, IUpdateHandler, IActivateSkinMaterialHandler
{
    private GameObject _bulletPrefab;

    private int _initialProjctilePoolSize = 10;
    private LocalObjectPool _projectilePool;

    private GameObject _wisp;

    private int _initialWispPoolSize = 3;
    private LocalObjectPool _wispPool;

    private int _wispLimit = 3;

    private bool _isRunningPassiveCoroutine = false;
    private bool _isRunningChargeCoroutine = false;

    private float _minBurnDamage = 5f;
    private float _currentburnDamage;
    private float _chargeTime = 3f;

    private int _maxChargeStage = 3;
    private int _currentChargeStage;

    private float _astralDuration = 2f;

    private float _passiveRadius = 10f;

    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.2f;
    public override float movementCooldown { get; } = 2f;
    public override float defenseCooldown { get; } = 5f;
    public override float specialCooldown { get; } = 1f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Fire);



    private void Start()
    {
        materialType = Type.Fire;

        Addressables.LoadAssetAsync<GameObject>("Wisp").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _wisp = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Wisp");
            }
        };

        //_wisp = Resources.Load<GameObject>("Fire/Wisp");

        _currentburnDamage = _minBurnDamage;
        //_bulletPrefab = projectilePrefabs[projectilePrefabKey];
    }

    #region Melee
    public override void MeleeAttack()
    {
        if (playerHealthController.OnDamageTaken == null)
        {
            playerHealthController.OnDamageTaken += HandleDamageTaken;
        }

        if (!_isRunningChargeCoroutine && _currentChargeStage < _maxChargeStage)
        {
            StartCoroutine(ActivateCharge(_chargeTime));
        }
    }

    private IEnumerator ActivateCharge(float time)
    {
        _isRunningChargeCoroutine = true;
        playerMovementController.disablingPlayerMove = true;
        playerAnimationController.DisablingPlayerAnimator = true;

        yield return new WaitForSeconds(time);

        playerMovementController.disablingPlayerMove = false;
        _currentburnDamage *= 1.5f;
        _currentChargeStage++;

        UIReferencesManager.Instance.FillChargeObjects[_currentChargeStage - 1].SetActive(true);

        _isRunningChargeCoroutine = false;
        playerAnimationController.DisablingPlayerAnimator = false;

    }

    private void HandleDamageTaken(float damage)
    {
        if (_currentburnDamage != _minBurnDamage && _currentChargeStage != 0)
        {
            _currentChargeStage = 0;

            foreach(GameObject stage in UIReferencesManager.Instance.FillChargeObjects)
            {
                Debug.Log("Charge active false");
                stage.SetActive(false);
            }

            _currentburnDamage = _minBurnDamage;
        }

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

        if (projectile != null && projectile.transform.childCount > 0)
        {
            for (int i = 0; i < projectile.transform.childCount; i++)
            {
                TrailRenderer child = projectile.transform.GetChild(i).GetComponent<TrailRenderer>();

                if (child != null)
                {
                    if (!child.enabled)
                    {
                        child.enabled = true;
                    }
                }
            }
        }

        projectile.GetComponent<BulletProjectile>().Movement(aimDir, () =>
        {
            _projectilePool.Release(projectile);

            if (projectile != null && projectile.transform.childCount > 0)
            {
                for (int i = 0; i < projectile.transform.childCount; i++)
                {
                    TrailRenderer child = projectile.transform.GetChild(i).GetComponent<TrailRenderer>();

                    if (child != null)
                    {
                        child.Clear();
                        child.enabled = false;
                    }
                }
            }
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
                TrailRenderer child = projectile.transform.GetChild(i).GetComponent<TrailRenderer>();
                if (child != null)
                {
                    if (!child.enabled)
                    {
                        child.enabled = true;
                    }
                }
            }
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

            bulletProjectile.Movement(aimDir, () =>
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
                                TrailRenderer child = projectile.transform.GetChild(i).GetComponent<TrailRenderer>();
                                if (child != null)
                                {
                                    child.Clear();
                                    child.enabled = false;
                                }
                            }
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
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Wisp"), QueryTriggerInteraction.Collide))
        {
            StartCoroutine(Teleport(hit.collider.gameObject.transform.position));
        }
    }

    private IEnumerator Teleport(Vector3 position)
    {
        playerMovementController.disablingPlayerJumpAndGravity = true;
        playerMovementController.disablingPlayerMove = true;

        playerNetworkTransform.Teleport(position, Player.transform.rotation, Player.transform.localScale);
        yield return new WaitForSeconds(0.2f);

        playerMovementController.disablingPlayerJumpAndGravity = false;
        playerMovementController.disablingPlayerMove = false;
    }

    #endregion

    #region Defense

    public override void Defense()
    {
        StartCoroutine(ActivateAstral());
    }

    private IEnumerator ActivateAstral()
    {
        EnableAstral();
        UpdateAstralStateRpc(true, ownerId, player.GetComponent<NetworkObject>());

        yield return new WaitForSeconds(_astralDuration);

        UpdateAstralStateRpc(false, ownerId, player.GetComponent<NetworkObject>());
        DisableAstral();
    }

    private void EnableAstral()
    {
        playerMovementController.disablingPlayerJumpAndGravity = true;
        playerMovementController.disablingPlayerMove = true;
        playerHealthController.healthStats.isImmortal = true;

        if (IsServer)
        {
            if (playerObjectReferences.FireModelNetwork.Value.TryGet(out NetworkObject fireModel))
            {
                fireModel.gameObject.SetActive(false);
            }
        }
        else
        {
            playerObjectReferences.FireModelLocal.SetActive(false);
        }
    }

    private void DisableAstral()
    {
        if (IsServer)
        {
            if (playerObjectReferences.FireModelNetwork.Value.TryGet(out NetworkObject fireModel))
            {
                fireModel.gameObject.SetActive(true);
            }
        }
        else
        {
            playerObjectReferences.FireModelLocal.SetActive(true);
        }

        playerHealthController.healthStats.isImmortal = false;
        playerMovementController.disablingPlayerJumpAndGravity = false;
        playerMovementController.disablingPlayerMove = false;
    }

    [Rpc(SendTo.Server)]
    private void UpdateAstralStateRpc(bool state, ulong id, NetworkObjectReference playerObjectReference)
    {
        UpdateAstralStateClientRpc(state, id, playerObjectReference);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateAstralStateClientRpc(bool state, ulong id, NetworkObjectReference playerObjectReference)
    {
        if (id != ownerId || (IsClient && !IsServer && (id == 0 && ownerId == 0)))
        {
            if (state)
            {
                if (playerObjectReference.TryGet(out NetworkObject playerObject))
                {
                    if (playerObject.GetComponent<PlayerObjectReferences>().FireModelNetwork.Value.TryGet(out NetworkObject fireModel))
                    {
                        fireModel.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                if (playerObjectReference.TryGet(out NetworkObject playerObject))
                {
                    if (playerObject.GetComponent<PlayerObjectReferences>().FireModelNetwork.Value.TryGet(out NetworkObject fireModel))
                    {
                        fireModel.gameObject.SetActive(true);
                    }
                }
            }
        }
    }

    #endregion

    #region Special

    private Queue<Wisp> _wispLimitedNetworkPoolForHost = new Queue<Wisp>();
    private Queue<Wisp> _wispLimitedNetworkPoolForClient = new Queue<Wisp>();

    private Queue<Wisp> _wispLimitedLocalPool = new Queue<Wisp>();

    public override void Special()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        RaycastHit hit;

        Vector3 wispSpawnPoint = Vector3.zero;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Ground"), QueryTriggerInteraction.Collide))
        {
            wispSpawnPoint = hit.point + hit.normal;
        }

        if (wispSpawnPoint != Vector3.zero)
        {
            if (!IsServer)
            {
                SpawnWispLocal(wispSpawnPoint);
            }

            SpawnWispServerRpc(wispSpawnPoint, ownerId);
        }
    }

    private void SpawnWispLocal(Vector3 wispSpawnPoint)
    {
        if (_wispPool == null)
        {
            if (_wisp != null)
            {
                _wispPool = new LocalObjectPool(_wisp, _initialWispPoolSize);
            }
        }

        Wisp wispObj = _wispPool.Get(wispSpawnPoint).GetComponent<Wisp>();

        _wispLimitedLocalPool.Enqueue(wispObj);

        wispObj.SetDeathAction(() => _wispPool.Release(wispObj.gameObject));

        if (_wispLimitedLocalPool.Count > _wispLimit)
        {
            _wispLimitedLocalPool.Dequeue().onDeathCallback?.Invoke();
        }
    }

    [Rpc(SendTo.Server)]
    private void SpawnWispServerRpc(Vector3 wispSpawnPoint, ulong ownerId)
    {
        NetworkObject wispNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_wisp, wispSpawnPoint);
        wispNetwork.Spawn();

        if (ownerId != 0)
        {
            wispNetwork.NetworkHide(ownerId);
        }

        Wisp wispObj = wispNetwork.GetComponent<Wisp>();

        if (ownerId == 0)
        {
            _wispLimitedNetworkPoolForHost.Enqueue(wispObj);
        }
        else
        {
            _wispLimitedNetworkPoolForClient.Enqueue(wispObj);
        }

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out NetworkClient client))
        {
            wispObj.owner = client.PlayerObject.GetComponent<PlayerSkillsController>();
        }

        wispObj.SetDeathAction(() =>
        {
            if (IsServer)
            {
                if (wispNetwork.IsSpawned)
                {
                    wispNetwork.Despawn();
                }
            }
        });

        if (_wispLimitedNetworkPoolForHost.Count > _wispLimit)
        {
            _wispLimitedNetworkPoolForHost.Dequeue().onDeathCallback?.Invoke();
        }

        if (_wispLimitedNetworkPoolForClient.Count > _wispLimit)
        {
            _wispLimitedNetworkPoolForClient.Dequeue().onDeathCallback?.Invoke();
        }
    }

    #endregion

    #region Passive

    public override void Passive()
    {
        Collider[] hitColliders = Physics.OverlapSphere(Player.transform.position, _passiveRadius, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                if (!_isRunningPassiveCoroutine)
                {
                    StartCoroutine(BurnCoroutine(_currentburnDamage));
                }
            }
        }
    }

    private IEnumerator BurnCoroutine(float damageNumber)
    {
        _isRunningPassiveCoroutine = true;

        playerSkillsController.enemyHealthController.TakeDamage(damageNumber);
        yield return new WaitForSeconds(.5f);

        _isRunningPassiveCoroutine = false;
    }

    #endregion

    public void ChangeSkinAction()
    {
        // Charge lvl reset
        _currentChargeStage = 0;

        foreach (GameObject stage in UIReferencesManager.Instance.FillChargeObjects)
        {
            stage.SetActive(false);
        }

        _currentburnDamage = _minBurnDamage;
        UIReferencesManager.Instance.FireChargeDisplayer.SetActive(false);

        if (playerObjectReferences.FireSkillRadius.activeSelf)
        {
            playerObjectReferences.FireSkillRadius.SetActive(false);
        }
    }

    public void HandleUpdate()
    {
        if (!playerObjectReferences.FireSkillRadius.activeSelf)
        {
            float visualSkillRaduis = _passiveRadius * 2f;
            playerObjectReferences.FireSkillRadius.transform.localScale = new Vector3(visualSkillRaduis, visualSkillRaduis, visualSkillRaduis);

            playerObjectReferences.FireSkillRadius.SetActive(true);
        }
    }

    public void ActivateSkinMaterialAction()
    {
        UIReferencesManager.Instance.FireChargeDisplayer.SetActive(true);
    }
}
