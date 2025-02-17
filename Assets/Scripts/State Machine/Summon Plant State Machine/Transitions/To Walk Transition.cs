using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToWalkTransition : AbstractTransition
{
    [SerializeField] private AbstractStateMachine stateMachine;

    private void Update()
    {
        if (stateMachine.summon.owner.enemy != null)
        {
            shouldTransition = true;
        }
    }
}
