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
            _animator.CrossFade(_walkStateHash, 0f);

            if (!stateMachine.summon.isNetworkObject)
            {
                var target = stateMachine.summon.owner.enemy;

                if (target != null)
                {
                    _agent.SetDestination(target.transform.position);
                }
                else
                {
                    _agent.ResetPath();
                }
            }
        }
        else
        {
            _animator.CrossFade(_walkStateHash, 0f);

            var target = stateMachine.summon.owner.enemy;

            if (target != null)
            {
                _agent.SetDestination(target.transform.position);
            }
            else
            {
                _agent.ResetPath();
            }
        }
    }

    public override void ExitState()
    {
        base.ExitState();
    }
}
