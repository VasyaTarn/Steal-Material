using UnityEngine;
using UnityEngine.AI;

public class WalkState : AbstractState
{
    [SerializeField] private NavMeshAgent _agent;
    [SerializeField] private Animator _animator;

    [SerializeField] private AbstractStateMachine stateMachine;

    private readonly int _walkStateHash = Animator.StringToHash("Walk");


    public override void StartState()
    {
        base.StartState();
    }

    private void Update()
    {
        if (IsClient && !IsServer)
        {
            if (!stateMachine.summon.isNetworkObject)
            {
                var target = stateMachine.summon.owner.enemy;

                if (target != null)
                {
                    _animator.CrossFade(_walkStateHash, 0.25f);
                    _agent.SetDestination(target.transform.position);
                }
            }
        }
        else
        {
            var target = stateMachine.summon.owner.enemy;

            if (target != null)
            {
                _animator.CrossFade(_walkStateHash, 0.25f);
                _agent.SetDestination(target.transform.position);
            }
        }
    }

    public override void ExitState()
    {
        base.ExitState();
    }
}
