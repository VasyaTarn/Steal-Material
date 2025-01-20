using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.UI.GridLayoutGroup;

public class WalkState : AbstractState
{
    [SerializeField] private NavMeshAgent _agent;


    private void Update()
    {
        if (IsClient && !IsServer)
        {
            if (!summon.isNetworkObject)
            {
                var target = summon.owner.enemy;
                if (target != null)
                {
                    _agent.SetDestination(target.transform.position);
                }
            }
        }
        else
        {
            var target = summon.owner.enemy;
            if (target != null)
            {
                _agent.SetDestination(target.transform.position);
            }
        }
    }
}
