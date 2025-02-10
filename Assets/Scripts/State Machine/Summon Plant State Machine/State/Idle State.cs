using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : AbstractState
{
    [SerializeField] private Animator _animator;

    [SerializeField] private AbstractStateMachine stateMachine;

    private readonly int _idleStateHash = Animator.StringToHash("Idle");

    public override void StartState()
    {
        base.StartState();
    }

    private void Update()
    {
        _animator.CrossFade(_idleStateHash, 0.25f);
    }

    public override void ExitState()
    {
        base.ExitState();
    }
}
