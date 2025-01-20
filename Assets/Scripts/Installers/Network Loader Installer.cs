using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class NetworkLoaderInstaller : MonoInstaller
{
    [SerializeField] private GameObject _networkManager;

    public override void InstallBindings()
    {
        DontDestroyOnLoad(Instantiate(_networkManager));
    }
}
