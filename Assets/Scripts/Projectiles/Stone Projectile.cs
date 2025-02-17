using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using Unity.Netcode;
using UnityEngine;

public class StoneProjectile : BulletProjectile
{
    private Tween _rotationTween;

    private void OnEnable()
    {
        if (_rotationTween == null)
        {
            _rotationTween = transform.DORotate(new Vector3(360, 360, 360), 2f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);
        }
        else
        {
            _rotationTween.Play();
        }
    }

    private void OnDisable()
    {
        if (_rotationTween != null)
        {
            _rotationTween.Kill();
            _rotationTween = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        OnTrigger(other);
    }

    void FixedUpdate()
    {
        Vector3 customGravity = new Vector3(0, -2f, 0);
        projectileRigidbody.AddForce(customGravity, ForceMode.Acceleration);
    }

    public override void Movement(Vector3 direction, Action releaseCallback)
    {
        if (projectileRigidbody == null)
        {
            projectileRigidbody = GetComponent<Rigidbody>();
        }

        onReleaseCallback = releaseCallback;

        projectileRigidbody.velocity = direction * speed;
    }

    protected override void OnTrigger(Collider target)
    {
        if (isNetworkObject && target.TryGetComponent(out PlayerHealthController healthController))
        {
            NetworkObject targetNetwork = target.gameObject.GetComponent<NetworkObject>();
            PlayerMovementController movementController = target.gameObject.GetComponent<PlayerMovementController>();

            if (healthController != null && targetNetwork.OwnerClientId != ownerId && movementController != null)
            {
                if (!movementController.currentMovementStats.isStuned.Value)
                {
                    healthController.TakeDamage(damage);
                }
                else
                {
                    healthController.TakeDamage(damage * 5);
                }
            }
        }

        onReleaseCallback?.Invoke();
    }
}
