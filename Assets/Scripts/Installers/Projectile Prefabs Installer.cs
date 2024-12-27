using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class ProjectilePrefabsInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.Bind<ProjectilePrefabs>().FromComponentInHierarchy().AsSingle();
    }
}
