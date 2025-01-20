using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PooledPrefabInstanceHandler : INetworkPrefabInstanceHandler
{
    private GameObject _prefab;
    private NetworkObjectPool _pool;

    public PooledPrefabInstanceHandler(GameObject prefab, NetworkObjectPool pool)
    {
        this._prefab = prefab;
        this._pool = pool;
    }

    NetworkObject INetworkPrefabInstanceHandler.Instantiate(ulong ownerClientId, Vector3 position, Quaternion rotation)
    {
        return _pool.GetNetworkObject(_prefab, position);
    }

    void INetworkPrefabInstanceHandler.Destroy(NetworkObject networkObject)
    {
        _pool.ReturnNetworkObject(networkObject, _prefab);
    }
}
