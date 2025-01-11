using System;
using Unity.Netcode;
using UnityEngine;

public class FireProjectile : BulletProjectile
{
    private Rigidbody projectileRigidbody;

    private void OnTriggerEnter(Collider other)
    {
        onTrigger(other);
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
        if (isNetworkObject)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f, LayerMask.GetMask("Player"));

            foreach (Collider collider in hitColliders)
            {
                NetworkObject playerNetworkObject = collider.gameObject.GetComponent<NetworkObject>();
                if (playerNetworkObject != null && playerNetworkObject.OwnerClientId != ownerId)
                {
                    playerNetworkObject.GetComponent<PlayerHealthController>().takeDamage(damage);
                }
            }
        }

        onReleaseCallback?.Invoke();
    }
}
