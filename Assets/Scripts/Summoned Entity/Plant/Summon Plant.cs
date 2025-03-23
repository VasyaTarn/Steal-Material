using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SummonPlant : SummonedEntity, IAttackable
{
    private GameObject _poisonExplosion;

    private int _initialPoisonExplosionPoolSize = 5;
    private LocalObjectPool _poisonExplosionPool;

    private Crosshair _crosshair;

    private void OnEnable()
    {
        StartCoroutine(DeathTimer(20f));
    }

    private void Start()
    {
        Addressables.LoadAssetAsync<GameObject>("Poison_explosion_summon").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _poisonExplosion = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Poison_explosion_summon");
            }
        };

        _crosshair = UIReferencesManager.Instance.Crosshair;
    }

    private void Update()
    {
        if (owner.enemy != null)
        {
            if (Vector3.Distance(transform.position, owner.enemy.transform.position) < 3f)
            {
                Attack(30f);
            }
        }
    }

    public void Attack(float damage)
    {
        if (isNetworkObject)
        {
            owner.enemyHealthController.TakeDamage(damage);
            owner.enemyMovementController.statusEffectsController.AddBuff(new Slowdown(0.5f, 2f));
            SpawnPoisonExplosionServerRpc(transform.position, owner.OwnerClientId);
        }
        else
        {
            SpawnPoisonExplosionLocal(transform.position);
            _crosshair.AnimateDamageResize();
        }

        onDeathCallback?.Invoke();
    }

    private IEnumerator DeathTimer(float duration)
    {
        yield return new WaitForSeconds(duration);

        if (isNetworkObject)
        {
            SpawnPoisonExplosionServerRpc(transform.position, owner.OwnerClientId);
        }
        else
        {
            SpawnPoisonExplosionLocal(transform.position);
        }

        onDeathCallback?.Invoke();
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
}
