using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SummonPlant : SummonedEntity, IAttackable
{
    private void Update()
    {
        if (IsClient && !IsServer)
        {
            if (!isNetworkObject)
            {
                if (Vector3.Distance(transform.position, owner.enemy.transform.position) < 5f)
                {
                    attack(20f);
                }
            }
        }
        else
        {
            if (Vector3.Distance(transform.position, owner.enemy.transform.position) < 5f)
            {
                attack(20f);
            }
        }
    }

    public void attack(float damage)
    {
        if (isNetworkObject)
        {
            owner.enemyHealthController.takeDamage(damage);
            owner.enemyMovementController.statusEffectsController.addBuff(new Slowdown(0.5f, 2f));
        }

        onDeathCallback?.Invoke();
    }
}
