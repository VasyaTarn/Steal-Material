using UnityEngine;

public class Stone : MaterialSkills
{
    public override void meleeAttack()
    {
        // Камень из земли перед игроком, при попадании подкидывает енеми 
        Debug.Log("Stone melee");
    }

    public override void rangeAttack(RaycastHit raycastHit)
    {
        // Брасок булыжника(в воздухе урон х2) + микростан
        Debug.Log("Stone range");
    }
}
