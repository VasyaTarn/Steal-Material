using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Pool;

public class NetworkObjectPool : NetworkBehaviour
{
    public static NetworkObjectPool Singleton { get; private set; }

    [SerializeField] private List<PoolConfigObject> pooledPrefabList;

    private HashSet<GameObject> prefabs = new HashSet<GameObject>();

    public Dictionary<GameObject, ObjectPool<NetworkObject>> pooledObjects = new Dictionary<GameObject, ObjectPool<NetworkObject>>();

    //private readonly Dictionary<GameObject, HashSet<NetworkObject>> activeObjects = new Dictionary<GameObject, HashSet<NetworkObject>>();

    private Vector3 spawnPoint;
    private ulong clientId;


    private void Awake()
    {
        if(Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        foreach (var configObject in pooledPrefabList)
        {
            RegisterPrefabInternal(configObject.prefab, configObject.prewarmCount);
        }
    }

    public override void OnNetworkDespawn()
    {
        foreach(var prefab in prefabs)
        {
            NetworkManager.Singleton.PrefabHandler.RemoveHandler(prefab);
            pooledObjects[prefab].Clear();
        }

        pooledObjects.Clear();
        prefabs.Clear();
    }

    public void OnValidate()
    {
        for(int i = 0; i < pooledPrefabList.Count; i++)
        {
            var prefab = pooledPrefabList[i].prefab;

            if(prefab != null)
            {
                Assert.IsNotNull(prefab.GetComponent<NetworkObject>(), $"{nameof(NetworkObjectPool)}: Pooled prefab \"{prefab.name}\" at index {i.ToString()} has no {nameof(NetworkObject)} component.");
            }
        }
    }

    public NetworkObject GetNetworkObject(GameObject prefab, Vector3 position)
    {
        spawnPoint = position;
        var networkObject = pooledObjects[prefab].Get();

        return networkObject;
    }

    public void ReturnNetworkObject(NetworkObject networkObject, GameObject prefab)
    {
        if (!pooledObjects.ContainsKey(prefab))
        {
            return;
        }

        pooledObjects[prefab].Release(networkObject);
    }

    private void RegisterPrefabInternal(GameObject prefab, int prewarmCount)
    {
        NetworkObject CreateFunc()
        {
            return Instantiate(prefab, spawnPoint, Quaternion.identity).GetComponent<NetworkObject>();
        }

        void ActionOnGet(NetworkObject networkObject)
        {
            networkObject.transform.position = spawnPoint;

            if (!networkObject.gameObject.activeSelf)
            {
                networkObject.gameObject.SetActive(true);
            }

            /*if(!activeObjects.ContainsKey(prefab))
            {
                activeObjects[prefab] = new HashSet<NetworkObject>();
            }
            activeObjects[prefab].Add(networkObject);*/
        }

        void ActionOnRelease(NetworkObject networkObject)
        {

            networkObject.gameObject.SetActive(false);

            /*if(activeObjects.ContainsKey(prefab))
            {
                activeObjects[prefab].Remove(networkObject);
            }*/
        }

        void ActionOnDestroy(NetworkObject networkObject)
        {
            Destroy(networkObject.gameObject);

            /*if (activeObjects.ContainsKey(prefab))
            {
                activeObjects[prefab].Remove(networkObject);
            }*/
        }

        prefabs.Add(prefab);

        pooledObjects[prefab] = new ObjectPool<NetworkObject>(CreateFunc, ActionOnGet, ActionOnRelease, ActionOnDestroy, false, prewarmCount);

        var prewarmNetworkObjects = new List<NetworkObject>();

        for(int i = 0; i < prewarmCount; i++)
        {
            prewarmNetworkObjects.Add(pooledObjects[prefab].Get());
        }
        foreach (var networkObject in prewarmNetworkObjects)
        {
            pooledObjects[prefab].Release(networkObject);
        }

        NetworkManager.Singleton.PrefabHandler.AddHandler(prefab, new PooledPrefabInstanceHandler(prefab, this));

    }

    /*public List<Wisp> GetActiveWisps(GameObject prefab)
    {
        if (activeObjects.TryGetValue(prefab, out var activeNetworkObjects))
        {
            return activeNetworkObjects
                .Select(networkObject => networkObject.GetComponent<Wisp>())
                .Where(wisp => wisp != null)
                .ToList();
        }

        return new List<Wisp>();
    }*/
}
