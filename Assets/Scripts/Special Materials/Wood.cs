using UnityEngine;

public class Wood : MaterialSkills
{
    public override void meleeAttack()
    {
        Debug.Log("Wood melee");
    }

    public override void rangeAttack(RaycastHit raycastHit)
    {
        Debug.Log("Wood range");
    }
}
