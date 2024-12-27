using UnityEngine;
using Unity.Netcode;
using System;

public abstract class BulletProjectile : NetworkBehaviour
{
    public float speed;
    protected bool isNetworkObject = false;

    public override void OnNetworkSpawn()
    {
        isNetworkObject = true;
    }

    public virtual void movement(Vector3 direction, Action releaseCallback) {}

    protected virtual void onTrigger(Collision target) {}


}
