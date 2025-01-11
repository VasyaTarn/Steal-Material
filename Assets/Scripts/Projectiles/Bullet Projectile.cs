using UnityEngine;
using Unity.Netcode;
using System;

public abstract class BulletProjectile : NetworkBehaviour
{
    [SerializeField] protected float speed;
    [SerializeField] protected float damage;


    protected bool isNetworkObject = false;
    protected Action onReleaseCallback;
    protected ulong ownerId;

    public override void OnNetworkSpawn()
    {
        isNetworkObject = true;
    }

    public virtual void movement(Vector3 direction, Action releaseCallback) {}

    protected virtual void onTrigger(Collider target) {}

    public void setOwnerId(ulong ownerId) 
    {
        this.ownerId = ownerId;
    }
}
