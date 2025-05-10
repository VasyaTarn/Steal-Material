using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class LocalProjectileManagerInstaller : MonoInstaller
{
    public LocalProjectileManager localProjectileManagerPrefab;

    public override void InstallBindings()
    {
        Container
            .Bind<LocalProjectileManager>()
            .FromComponentInNewPrefab(localProjectileManagerPrefab)
            .AsSingle()
            .NonLazy();
    }
}
