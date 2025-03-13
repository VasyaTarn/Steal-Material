using System;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

public class BasicProjectile : BulletProjectile
{
    private void OnTriggerEnter(Collider other)
    {
        OnTrigger(other);
    }

    public override void Movement(Vector3 direction, Action releaseCallback)
    {
        if(projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        onReleaseCallback = releaseCallback;

        projectileRigidbody.velocity = direction * speed;
    }

    protected override void OnTrigger(Collider target)
    {
        if (target.TryGetComponent(out PlayerHealthController healthController))
        {
            if (healthController != null)
            {
                if (isNetworkObject)
                {

                    healthController.TakeDamage(damage);
                }
                else
                {
                    _crosshair.AnimateDamageResize();
                }
            }
        }

        

        onReleaseCallback?.Invoke();
    }
}
