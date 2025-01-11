using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Netcode;
using UnityEngine;

public class StoneProjectile : BulletProjectile
{
    private Rigidbody projectileRigidbody;

    private void OnTriggerEnter(Collider other)
    {
        onTrigger(other);
    }

    void FixedUpdate()
    {
        Vector3 customGravity = new Vector3(0, -2f, 0);
        projectileRigidbody.AddForce(customGravity, ForceMode.Acceleration);
    }

    public override void movement(Vector3 direction, Action releaseCallback)
    {
        if (projectileRigidbody == null)
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
            NetworkObject targetNetwork = target.gameObject.GetComponent<NetworkObject>();
            PlayerMovementController movementController = target.gameObject.GetComponent<PlayerMovementController>();

            if (healthController != null && targetNetwork.OwnerClientId != ownerId && movementController != null)
            {
                if (!movementController.currentMovementStats.isStuned.Value)
                {
                    healthController.takeDamage(damage);
                }
                else
                {
                    healthController.takeDamage(damage * 5);
                }
            }
        }

        onReleaseCallback?.Invoke();
    }
}
