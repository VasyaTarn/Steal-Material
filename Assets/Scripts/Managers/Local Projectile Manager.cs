using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Zenject;

public class LocalProjectileManager : MonoBehaviour
{
    private static LocalProjectileManager _instance;

    public static LocalProjectileManager Instance => _instance;


    [Inject]
    public void Construct()
    {
        _instance = this;
    }

    [SerializeField] private AssetReference explosionPrefabReference;
    private LocalObjectPool _explosionPool;

    private void Start()
    {
        if(NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            LoadExplosionPrefab();
        }
    }

    public void LoadExplosionPrefab()
    {
        explosionPrefabReference.LoadAssetAsync<GameObject>().Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject explosionPrefab = handle.Result;
                _explosionPool = new LocalObjectPool(explosionPrefab, 5);

                PrewarmPool();
            }
        };
    }

    private void PrewarmPool()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject obj = _explosionPool.Get(Vector3.zero);
            _explosionPool.Release(obj);
        }
    }

    public GameObject GetExplosionFromPool(Vector3 position)
    {
        return _explosionPool?.Get(position);
    }

    public LocalObjectPool GetPool()
    {
        return _explosionPool;
    }

    private void OnDestroy()
    {
        explosionPrefabReference.ReleaseAsset();
    }
}
