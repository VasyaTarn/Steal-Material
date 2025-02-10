using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbstractState : NetworkBehaviour
{
    [SerializeField] private AbstractTransition[] _transitions;

    //[SerializeField] protected SummonedEntity summon;


    public virtual void StartState()
    {
        if(!enabled)
        {
            enabled = true;

            foreach (var transition in _transitions)
            {
                transition.enabled = true;
            }
        }
    }

    public virtual void ExitState()
    {
        if(enabled)
        {
            enabled = false;

            foreach (var transition in _transitions)
            {
                transition.enabled = false;
            }
        }
    }

    public AbstractState GetNextState()
    {
        foreach (var transition in _transitions)
        {
            if(transition.shouldTransition)
            {
                return transition.StateToTransition;
            }
        }

        return null;
    }
}
