using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ProjectilePrefabs : MonoBehaviour
{
    private Dictionary<string, GameObject> projectilePrefabs = new Dictionary<string, GameObject>();

    [Inject]
    public void Initialize()
    {
        GameObject[] arrPrefabs = Resources.LoadAll<GameObject>("Projectile Prefabs");
        foreach (GameObject prefab in arrPrefabs)
        {
            projectilePrefabs.Add(prefab.name, prefab);
        }
    }

    public Dictionary<string, GameObject> getProjectilePrefabs()
    {
        return projectilePrefabs;
    }

}
