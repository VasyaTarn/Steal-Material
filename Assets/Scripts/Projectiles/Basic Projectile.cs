using System;
using Unity.Netcode;
using UnityEngine;

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
            NetworkObject player = target.GetComponent<NetworkObject>();

            if (healthController != null)
            {
                if (isNetworkObject)
                {
                    if (player.OwnerClientId != ownerId)
                    {
                        healthController.TakeDamage(damage);
                    }
                }
                else
                {
                    _crosshair.AnimateDamageResize();
                }
            }
        }

        if (!target.CompareTag("CapturePoint"))
        {
            onReleaseCallback?.Invoke();
        }
    }
}
