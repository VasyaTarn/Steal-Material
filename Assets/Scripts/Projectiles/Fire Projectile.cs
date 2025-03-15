using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FireProjectile : BulletProjectile
{
    private GameObject _explosion;

    private int _initialexplosionPoolSize = 10;
    private LocalObjectPool _explosionPool;

    protected override void Start()
    {
        base.Start();

        Addressables.LoadAssetAsync<GameObject>("Explosion").Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _explosion = handle.Result;
            }
            else
            {
                Debug.LogError("Failed to load Explosion");
            }
        };

        //_explosion = Resources.Load<GameObject>("Fire/Explosion");
    }

    private void OnTriggerEnter(Collider other)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position - projectileRigidbody.velocity.normalized * 0.1f, projectileRigidbody.velocity.normalized, out hit, 1f))
        {
            if (!IsServer)
            {
                SpawnExplosionLocal(hit.point + hit.normal * 3.5f);
            }

            SpawnExplosionServerRpc(hit.point + hit.normal * 3.5f, ownerId);
        }

        OnTrigger(other);
    }

    public override void Movement(Vector3 direction, Action releaseCallback)
    {
        if (projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        onReleaseCallback = releaseCallback;

        projectileRigidbody.velocity = direction * speed;
    }

    protected override void OnTrigger(Collider target)
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Player"));

        foreach (Collider collider in hitColliders)
        {
            NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null)
            {
                if (isNetworkObject)
                {
                    if (playerNetworkObject.OwnerClientId != ownerId)
                    {
                        playerNetworkObject.GetComponent<PlayerHealthController>().TakeDamage(damage, ownerId);
                    }
                }
                else
                {
                    
                    _crosshair.AnimateDamageResize();
                }
            }
        }

        onReleaseCallback?.Invoke();
    }

    private void SpawnExplosionLocal(Vector3 explosionSpawnPoint)
    {
        if (_explosionPool == null)
        {
            if (_explosion != null)
            {
                _explosionPool = new LocalObjectPool(_explosion, _initialexplosionPoolSize);
            }
        }

        GameObject explosionburst = _explosionPool.Get(explosionSpawnPoint);

        ReleaseExplosionAsync(0.5f, () => _explosionPool.Release(explosionburst)).Forget();
    }

    [Rpc(SendTo.Server)]
    private void SpawnExplosionServerRpc(Vector3 explosionSpawnPoint, ulong ownerId)
    {
        NetworkObject explosionNetwork = NetworkObjectPool.Singleton.GetNetworkObject(_explosion, explosionSpawnPoint);
        explosionNetwork.Spawn();

        if (ownerId != 0)
        {
            explosionNetwork.NetworkHide(ownerId);
        }

        ReleaseExplosionAsync(0.5f, () =>
        {
            if (IsServer)
            {
                if (explosionNetwork.IsSpawned)
                {
                    explosionNetwork.Despawn();
                }
            }
        }).Forget();
    }

    private async UniTask ReleaseExplosionAsync(float duration, Action releaseAction)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(duration), cancellationToken: this.GetCancellationTokenOnDestroy());
        releaseAction?.Invoke();
    }

    /*private IEnumerator ReleaseExplosion(float duration, Action releaseAction)
    {
        yield return new WaitForSeconds(duration);
        releaseAction?.Invoke();
        Debug.Log("Test");
    }*/
}
