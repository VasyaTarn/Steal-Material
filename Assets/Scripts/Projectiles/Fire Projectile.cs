using Cysharp.Threading.Tasks;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class FireProjectile : BulletProjectile
{
    private GameObject _explosion;

    private void OnDisable()
    {
        if (_explosion == null)
        {
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
        }
    }

    protected override void Start()
    {
        base.Start();

        //_explosion = Resources.Load<GameObject>("Fire/Explosion");
    }

    private void OnTriggerEnter(Collider other)
    {
        RaycastHit hit;
        if (projectileRigidbody != null)
        {
            if (Physics.Raycast(transform.position - projectileRigidbody.velocity.normalized * 0.1f, projectileRigidbody.velocity.normalized, out hit, 1f))
            {
                if (!IsServer)
                {
                    if (!other.CompareTag("CapturePoint"))
                    {
                        SpawnExplosionLocal(hit.point + hit.normal * 3.5f);
                    }
                }

                if (!other.CompareTag("CapturePoint"))
                {
                    SpawnExplosionServerRpc(hit.point + hit.normal * 3.5f, ownerId);
                }
            }
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

        if (!target.CompareTag("CapturePoint"))
        {
            onReleaseCallback?.Invoke();
        }
    }

    private void SpawnExplosionLocal(Vector3 explosionSpawnPoint)
    {
        GameObject explosionburst = LocalProjectileManager.Instance.GetExplosionFromPool(explosionSpawnPoint);

        ReleaseExplosionAsync(0.5f, () => LocalProjectileManager.Instance.GetPool().Release(explosionburst)).Forget();
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
}
