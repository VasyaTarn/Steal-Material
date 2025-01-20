using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ProjectilePrefabs : MonoBehaviour
{
    private Dictionary<string, GameObject> _projectilePrefabs = new Dictionary<string, GameObject>();

    [Inject]
    public void Initialize()
    {
        GameObject[] arrPrefabs = Resources.LoadAll<GameObject>("Projectile Prefabs");
        foreach (GameObject prefab in arrPrefabs)
        {
            _projectilePrefabs.Add(prefab.name, prefab);
        }
    }

    public Dictionary<string, GameObject> GetProjectilePrefabs()
    {
        return _projectilePrefabs;
    }
}
