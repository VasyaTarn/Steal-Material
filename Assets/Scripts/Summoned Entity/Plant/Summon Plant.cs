using UnityEngine;

public class SummonPlant : SummonedEntity
{
    private void Update()
    {
        if(Vector3.Distance(transform.position, owner.getEnemy().transform.position) < 5f)
        {
            attack(20f);
        }
    }

    protected override void attack(float damage)
    {
        owner.getEnemyHealthController().takeDamage(damage);
        onDeathCallback?.Invoke();
    }
}
