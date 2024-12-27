using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class NetworkLoaderInstaller : MonoInstaller
{
    [SerializeField] private GameObject networkManager;

    public override void InstallBindings()
    {
        DontDestroyOnLoad(Instantiate(networkManager));
    }
}
