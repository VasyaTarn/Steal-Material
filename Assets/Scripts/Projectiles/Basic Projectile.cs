using System;
using Unity.Netcode;
using UnityEngine;

public class BasicProjectile : BulletProjectile
{
    private Rigidbody projectileRigidbody;

    private Action onReleaseCallback;

    private void OnCollisionEnter(Collision collision)
    {
        onTrigger(collision);
    }

    public override void movement(Vector3 direction, Action releaseCallback)
    {
        if(projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        onReleaseCallback = releaseCallback;

        projectileRigidbody.velocity = direction * speed;
    }

    protected override void onTrigger(Collision target)
    {
        if (isNetworkObject && target.gameObject.CompareTag("Player"))
        {
            PlayerHealthController healthController = target.gameObject.GetComponent<PlayerHealthController>();

            if (healthController != null)
            {
                healthController.takeDamage(10);
            }
        }

        onReleaseCallback?.Invoke();
    }
}
