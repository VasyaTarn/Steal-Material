using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbstractStateMachine : MonoBehaviour
{
    [SerializeField] private AbstractState _startState;

    private AbstractState _currentState;

    public SummonedEntity summon;


    private void Awake()
    {
        _currentState = _startState;
    }

    private void Start()
    {
        _currentState.StartState();
    }

    private void Update()
    {
        if(_currentState == null)
        {
            return;
        }

        AbstractState nextState = _currentState.GetNextState();

        if(nextState != null)
        {
            Transition(nextState);
        }
    }

    private void Transition(AbstractState nextState)
    {
        if (_currentState != null)
        {
            _currentState.ExitState();
        }

        _currentState = nextState;

        _currentState.StartState();


    }
}
