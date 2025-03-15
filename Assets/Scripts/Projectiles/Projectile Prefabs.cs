using System.Collections.Generic;
using UnityEngine;
using Zenject;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ProjectilePrefabs : MonoBehaviour
{
    private Dictionary<string, GameObject> _projectilePrefabs = new Dictionary<string, GameObject>();

    [Inject]
    public void Initialize()
    {
        Addressables.LoadAssetsAsync<GameObject>("projectile_prefabs", OnPrefabLoaded).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                Debug.Log($"Loaded {handle.Result.Count} projectile prefabs.");
            }
            else
            {
                Debug.LogError("Failed to load projectile prefabs.");
            }
        };

        /*GameObject[] arrPrefabs = Resources.LoadAll<GameObject>("Projectile Prefabs");
        foreach (GameObject prefab in arrPrefabs)
        {
            _projectilePrefabs.Add(prefab.name, prefab);
        }*/
    }

    private void OnPrefabLoaded(GameObject prefab)
    {
        _projectilePrefabs.Add(prefab.name, prefab);
        //_projectilePrefabs[prefab.name] = prefab;
    }

    public Dictionary<string, GameObject> GetProjectilePrefabs()
    {
        return _projectilePrefabs;
    }
}
