using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SummonPlant : SummonedEntity, IAttackable
{
    private void Update()
    {
        if (owner.enemy != null)
        {
            if (IsClient && !IsServer)
            {
                if (!isNetworkObject)
                {
                    if (Vector3.Distance(transform.position, owner.enemy.transform.position) < 5f)
                    {
                        Attack(20f);
                    }
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, owner.enemy.transform.position) < 5f)
                {
                    Attack(20f);
                }
            }
        }
    }

    public void Attack(float damage)
    {
        if (isNetworkObject)
        {
            owner.enemyHealthController.TakeDamage(damage);
            owner.enemyMovementController.statusEffectsController.AddBuff(new Slowdown(0.5f, 2f));
        }

        onDeathCallback?.Invoke();
    }
}
