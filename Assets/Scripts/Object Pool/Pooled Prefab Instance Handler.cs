using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    private GameObject prefab;
    private NetworkObjectPool pool;

    public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
    {
        this.prefab = prefab;
        this.pool = pool;
    }

    NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        return pool.GetNetworkObject(prefab, position);
    }

    void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
    {
        pool.ReturnNetworkObject(networkObject, prefab);
    }
}
