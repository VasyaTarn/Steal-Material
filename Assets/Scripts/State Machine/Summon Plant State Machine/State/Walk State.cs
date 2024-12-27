using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class WalkState : AbstractState
{
    [SerializeField] private NavMeshAgent agent;

    private void Update()
    {
        var target = summon.owner.getEnemy();
        if(target != null)
        {
            agent.SetDestination(target.transform.position);
        }
    }
}
