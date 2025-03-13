using UnityEngine;
using Unity.Netcode;
using System;

public abstract class BulletProjectile : NetworkBehaviour
{
    protected Rigidbody projectileRigidbody;

    [SerializeField] protected float speed;
    [SerializeField] protected float damage;


    public bool isNetworkObject = false;
    protected Action onReleaseCallback;
    protected ulong ownerId;

    public Crosshair _crosshair;

    protected virtual void Start()
    {
        _crosshair = UIReferencesManager.Instance.Crosshair;
    }

    public override void OnNetworkSpawn()
    {
        isNetworkObject = true;
    }

    public abstract void Movement(Vector3 direction, Action releaseCallback);

    protected abstract void OnTrigger(Collider target);

    public void SetOwnerId(ulong ownerId)
    {
        this.ownerId = ownerId;
    }
}
