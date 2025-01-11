using System;
using System.Collections;
using System.Drawing;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UI.Image;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;

public class Fire : MaterialSkills
{
    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.2f;
    public override float movementCooldown { get; } = 2f;
    public override float defenseCooldown { get; } = 5f;
    public override float specialCooldown { get; } = 0f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Fire);

    private GameObject bulletPrefab;

    private int initialProjctilePoolSize = 10;
    private LocalObjectPool projectilePool;

    private GameObject wisp;

    private int initialWispPoolSize = 10;
    private LocalObjectPool wispPool;

    private int wispLimit = 3;

    private bool isRunningPassiveCoroutine = false;
    private bool isRunningChargeCoroutine = false;

    private float minBurnDamage = 5f;
    private float currentburnDamage;
    private float chargeTime = 3f;

    private int maxChargeStage = 3;
    private int currentChargeStage;

    private float astralDuration = 2f;

    private void Start()
    {
        wisp = Resources.Load<GameObject>("Fire/Wisp");
        currentburnDamage = minBurnDamage;
        bulletPrefab = projectilePrefabs[projectilePrefabKey];
    }

    public override void meleeAttack()
    {
        if(playerHealthController.OnDamageTaken == null)
        {
            playerHealthController.OnDamageTaken += HandleDamageTaken;
        }

        if (!isRunningChargeCoroutine && currentChargeStage < maxChargeStage)
        {
            StartCoroutine(activateCharge(chargeTime));
        }
    }

    private IEnumerator activateCharge(float time)
    {
        isRunningChargeCoroutine = true;
        disablingPlayerMove = true;

        yield return new WaitForSeconds(time);

        disablingPlayerMove = false;
        currentburnDamage *= 1.5f;
        currentChargeStage++;
        isRunningChargeCoroutine = false;

    }

    private void HandleDamageTaken(float damage)
    {
        if (currentburnDamage != minBurnDamage && currentChargeStage != 0)
        {
            currentChargeStage = 0;
            currentburnDamage = minBurnDamage;
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

            bulletProjectile.movement(aimDir, () =>
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
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = Camera.main.ScreenPointToRay(screenCenterPoint);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, LayerMask.GetMask("Wisp"), QueryTriggerInteraction.Collide))
        {
            StartCoroutine(teleport(hit.collider.gameObject.transform.position));
        }
    }

    private IEnumerator teleport(Vector3 position)
    {
        disablingPlayerJumpAndGravity = true;
        disablingPlayerMove = true;

        playerNetworkTransform.Teleport(position, player.transform.rotation, player.transform.localScale);
        yield return new WaitForSeconds(0.2f);

        disablingPlayerJumpAndGravity = false;
        disablingPlayerMove = false;
    }

    public override void defense()
    {
        StartCoroutine(activateAstral());
    }

    private IEnumerator activateAstral()
    {
        enableAstral();
        updateAstralStateRpc(true, ownerId);

        yield return new WaitForSeconds(astralDuration);

        updateAstralStateRpc(false, ownerId);
        disableAstral();
    }

    private void enableAstral()
    {
        disablingPlayerJumpAndGravity = true;
        disablingPlayerMove = true;
        playerHealthController.healthStats.isImmortal = true;

        playerSkillsController.model.SetActive(false);
        playerHealthController.healthbarSprite.gameObject.SetActive(false);
    }

    private void disableAstral()
    {
        playerSkillsController.model.SetActive(true);
        playerHealthController.healthbarSprite.gameObject.SetActive(true);
        playerHealthController.healthStats.isImmortal = false;
        disablingPlayerJumpAndGravity = false;
        disablingPlayerMove = false;
    }

    [Rpc(SendTo.Server)]
    private void updateAstralStateRpc(bool state, ulong id)
    {
        updateAstralStateClientRpc(state, id);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void updateAstralStateClientRpc(bool state, ulong id)
    {
        if (id != ownerId)
        {
            if (state)
            {
                playerSkillsController.enemySkillsController.model.SetActive(false);
                playerSkillsController.enemyHealthController.healthbarSprite.gameObject.SetActive(false);
            }
            else
            {
                playerSkillsController.enemySkillsController.model.SetActive(true);
                playerSkillsController.enemyHealthController.healthbarSprite.gameObject.SetActive(true);
            }
        }
    }

    private Queue<Wisp> wispLimitedNetworkPoolForHost = new Queue<Wisp>();
    private Queue<Wisp> wispLimitedNetworkPoolForClient = new Queue<Wisp>();

    private Queue<Wisp> wispLimitedLocalPool = new Queue<Wisp>();

    public override void special()
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
                spawnWispLocal(wispSpawnPoint);
            }

            spawnWispServerRpc(wispSpawnPoint, ownerId);
        }

        /*foreach (Wisp wisp in NetworkObjectPool.Singleton.GetActiveWisps(wisp))
        {
            Debug.Log(wisp);
            if(wisp.owner.OwnerClientId == ownerId && !wispLimitedNetworkPool.Contains(wisp))
            {
                wispLimitedNetworkPool.Enqueue(wisp);
            }
        }

        while(wispLimitedNetworkPool.Count > wispLimit)
        {
            wispLimitedNetworkPool.Dequeue().onDeathCallback?.Invoke();
        }*/
    }

    private void spawnWispLocal(Vector3 wispSpawnPoint)
    {
        if (wispPool == null)
        {
            if (wisp != null)
            {
                wispPool = new LocalObjectPool(wisp, initialWispPoolSize);
            }
        }

        Wisp wispObj = wispPool.Get(wispSpawnPoint).GetComponent<Wisp>();

        wispLimitedLocalPool.Enqueue(wispObj);

        wispObj.setDeathAction(() => wispPool.Release(wispObj.gameObject));

        if(wispLimitedLocalPool.Count > wispLimit)
        {
            wispLimitedLocalPool.Dequeue().onDeathCallback?.Invoke();
        }
    }

    [Rpc(SendTo.Server)]
    private void spawnWispServerRpc(Vector3 wispSpawnPoint, ulong ownerId)
    {
        NetworkObject wispNetwork = NetworkObjectPool.Singleton.GetNetworkObject(wisp, wispSpawnPoint);
        wispNetwork.Spawn();

        if (ownerId != 0)
        {
            wispNetwork.NetworkHide(ownerId);
        }

        Wisp wispObj = wispNetwork.GetComponent<Wisp>();

        if(ownerId == 0)
        {
            wispLimitedNetworkPoolForHost.Enqueue(wispObj);
        }
        else
        {
            wispLimitedNetworkPoolForClient.Enqueue(wispObj);
        }

        /*Debug.Log("-------Client------");

        foreach (Wisp wisp in wispLimitedNetworkPoolForClient)
        {
            Debug.Log(wisp);
        }

        Debug.Log("-------------------");

        Debug.Log("-------Host------");

        foreach (Wisp wisp in wispLimitedNetworkPoolForHost)
        {
            Debug.Log(wisp);
        }

        Debug.Log("-------------------");*/

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerId, out NetworkClient client))
        {
            wispObj.owner = client.PlayerObject.GetComponent<PlayerSkillsController>();
        }

        wispObj.setDeathAction(() =>
        {
            if (IsServer)
            {
                if (wispNetwork.IsSpawned)
                {
                    wispNetwork.Despawn();
                }
            }
        });

        if (wispLimitedNetworkPoolForHost.Count > wispLimit)
        {
            wispLimitedNetworkPoolForHost.Dequeue().onDeathCallback?.Invoke();
        }

        if (wispLimitedNetworkPoolForClient.Count > wispLimit)
        {
            wispLimitedNetworkPoolForClient.Dequeue().onDeathCallback?.Invoke();
        }
    }

    public override void passive()
    {
        Collider[] hitColliders = Physics.OverlapSphere(player.transform.position, 10f, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
            {
                if(!isRunningPassiveCoroutine)
                {
                    StartCoroutine(burnCoroutine(currentburnDamage));
                }
            }
        }
    }

    private IEnumerator burnCoroutine(float damageNumber)
    {
        isRunningPassiveCoroutine = true;

        playerSkillsController.enemyHealthController.takeDamage(damageNumber);
        yield return new WaitForSeconds(.5f);

        isRunningPassiveCoroutine = false;
    }
}
