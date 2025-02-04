using UnityEngine;
using Unity.Netcode;
using System;

public abstract class BulletProjectile : NetworkBehaviour
{
    protected Rigidbody projectileRigidbody;

    [SerializeField] protected float speed;
    [SerializeField] protected float damage;


    protected bool isNetworkObject = false;
    protected Action onReleaseCallback;
    protected ulong ownerId;

    public override void OnNetworkSpawn()
    {
        isNetworkObject = true;
    }

    public virtual void Movement(Vector3 direction, Action releaseCallback) {}

    protected virtual void OnTrigger(Collider target) {}

    public void SetOwnerId(ulong ownerId)
    {
        this.ownerId = ownerId;
    }
}
