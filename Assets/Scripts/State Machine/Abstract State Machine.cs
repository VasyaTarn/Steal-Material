using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractStateMachine : MonoBehaviour
{
    [SerializeField] private AbstractState startState;

    private AbstractState currentState;

    private void Awake()
    {
        currentState = startState;
    }

    private void Start()
    {
        currentState.StartState();
    }

    private void Update()
    {
        if(currentState == null)
        {
            return;
        }

        AbstractState nextState = currentState.GetNextState();

        if(nextState != null)
        {
            Transition(nextState);
        }
    }

    private void Transition(AbstractState nextState)
    {
        if (currentState != null)
        {
            currentState.ExitState();
        }

        currentState = nextState;

        currentState.StartState();


    }
}
