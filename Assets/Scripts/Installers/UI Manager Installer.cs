using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class UIManagerInstaller : MonoInstaller
{
    [SerializeField] private UIReferencesManager _uiManager;

    public override void InstallBindings()
    {
        Container.Bind<UIReferencesManager>().FromInstance(_uiManager).AsSingle().NonLazy();
    }
}
