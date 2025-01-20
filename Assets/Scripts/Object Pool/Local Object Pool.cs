using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class LocalObjectPool
{
    private ObjectPool<GameObject> _pool;

    private GameObject _prefab;

    private Vector3 _spawnPoint;

    public LocalObjectPool(GameObject prefab, int objectsCount)
    {
        this._prefab = prefab;
        _pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy, false, objectsCount);
    }

    public GameObject Get(Vector3 position)
    {
        _spawnPoint = position;
        GameObject obj = _pool.Get();

        return obj;
    }

    public void Release(GameObject obj)
    {
        _pool.Release(obj);
    }

    private GameObject OnCreate()
    {
        GameObject obj = GameObject.Instantiate(_prefab, _spawnPoint, Quaternion.identity);
        return obj;
    }

    private void OnGet(GameObject obj)
    {
        obj.transform.position = _spawnPoint;
        obj.gameObject.SetActive(true);
    }

    private void OnRelease(GameObject obj)
    {
        obj.gameObject.SetActive(false);
    }

    private void OnDestroy(GameObject obj)
    {
        NetworkObject networkObj = obj.GetComponent<NetworkObject>();
        GameObject.Destroy(obj.gameObject);
    }
}
