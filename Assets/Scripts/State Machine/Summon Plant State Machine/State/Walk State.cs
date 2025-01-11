using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class WalkState : AbstractState
{
    [SerializeField] private NavMeshAgent agent;

    private void Update()
    {
        /*if (IsServer)
        {
            var target = summon.owner.getEnemy();
            if (target != null)
            {
                agent.SetDestination(target.transform.position);
            }
        }*/

        if (IsClient && !IsServer)
        {
            if (!summon.isNetworkObject)
            {
                var target = summon.owner.enemy;
                if (target != null)
                {
                    agent.SetDestination(target.transform.position);
                }
            }
        }
        else
        {
            var target = summon.owner.enemy;
            if (target != null)
            {
                agent.SetDestination(target.transform.position);
            }
        }
    }
}
