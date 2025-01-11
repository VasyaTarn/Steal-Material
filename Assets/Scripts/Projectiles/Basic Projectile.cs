using System;
using Unity.Netcode;
using UnityEngine;

public class BasicProjectile : BulletProjectile
{
    private Rigidbody projectileRigidbody;

    private void OnTriggerEnter(Collider other)
    {
        onTrigger(other);
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

    protected override void onTrigger(Collider target)
    {

        if (isNetworkObject && target.gameObject.CompareTag("Player"))
        {
            PlayerHealthController healthController = target.gameObject.GetComponent<PlayerHealthController>();

            if (healthController != null)
            {
                healthController.takeDamage(damage);
            }
        }

        onReleaseCallback?.Invoke();
    }
}
