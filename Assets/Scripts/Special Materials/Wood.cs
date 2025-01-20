using UnityEngine;

public class Wood : MaterialSkills
{
    public override void MeleeAttack()
    {
        Debug.Log("Wood melee");
    }

    public override void RangeAttack(RaycastHit raycastHit)
    {
        Debug.Log("Wood range");
    }
}
