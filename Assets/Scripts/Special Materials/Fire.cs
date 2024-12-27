using System;
using System.Collections;
using UnityEngine;

public class Fire : MaterialSkills
{
    private bool canDash = true;
    private float dashingPower = 12f;
    private float dashingTime = 0.5f;
    private float dashCd = 1f;
    private Vector3 dashVelocity;

    public override float meleeAttackCooldown { get; } = 0.5f;
    public override float rangeAttackCooldown { get; } = 0.2f;

    public override string projectilePrefabKey { get; } = ProjectileMapper.GetProjectileKey(ProjectileType.Fire);

    public override void meleeAttack()
    {
        Debug.Log("Fire melee");
    }

    public override void rangeAttack(RaycastHit raycastHit)
    {
        Debug.Log("Fire range");
    }

    public override void movement()
    {
        if(canDash)
        {
            StartCoroutine(dash());
        }
    }

    IEnumerator dash()
    {
        canDash = false;
        disablingPlayerMoveDuringMovementSkill = true;
        dashVelocity = playerMovementController.transform.forward * dashingPower;
        float startTime = Time.time;

        while (Time.time < startTime + dashingTime)
        {
            playerMovementController.controller.Move(dashVelocity * Time.deltaTime);
            yield return null;
        }

        disablingPlayerMoveDuringMovementSkill = false;
        yield return new WaitForSeconds(dashCd);
        canDash = true;
    }
}
