using Unity.Netcode;
using UnityEngine;
using UnityEngine.Pool;

public class LocalObjectPool
{
    private ObjectPool<GameObject> pool;

    private GameObject prefab;

    private Vector3 spawnPoint;

    public LocalObjectPool(GameObject prefab, int objectsCount)
    {
        this.prefab = prefab;
        pool = new ObjectPool<GameObject>(OnCreate, OnGet, OnRelease, OnDestroy, false, objectsCount);
    }

    public GameObject Get(Vector3 position)
    {
        spawnPoint = position;
        GameObject obj = pool.Get();

        return obj;
    }

    public void Release(GameObject obj)
    {
        pool.Release(obj);
    }

    private GameObject OnCreate()
    {
        GameObject obj = GameObject.Instantiate(prefab, spawnPoint, Quaternion.identity);
        return obj;
    }

    private void OnGet(GameObject obj)
    {
        obj.transform.position = spawnPoint;
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
