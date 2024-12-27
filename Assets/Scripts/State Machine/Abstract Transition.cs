using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractTransition : MonoBehaviour
{
    [SerializeField] private AbstractState stateToTransition;

    public AbstractState StateToTransition => stateToTransition;
    public bool shouldTransition { get; set; }

    private void OnEnable()
    {
        shouldTransition = false;
    }
}
