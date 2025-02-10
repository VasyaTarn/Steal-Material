using UnityEngine;

public class Wood : MaterialSkills
{
    public override void Defense()
    {
        throw new System.NotImplementedException();
    }

    public override void MeleeAttack()
    {
        Debug.Log("Wood melee");
    }

    public override void Movement()
    {
        throw new System.NotImplementedException();
    }

    public override void Passive()
    {
        throw new System.NotImplementedException();
    }

    public override void RangeAttack(RaycastHit raycastHit)
    {
        Debug.Log("Wood range");
    }

    public override void Special()
    {
        throw new System.NotImplementedException();
    }
}
