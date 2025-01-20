using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractTransition : MonoBehaviour
{
    [SerializeField] private AbstractState _stateToTransition;

    public AbstractState StateToTransition => _stateToTransition;
    public bool shouldTransition { get; set; }


    private void OnEnable()
    {
        shouldTransition = false;
    }
}
