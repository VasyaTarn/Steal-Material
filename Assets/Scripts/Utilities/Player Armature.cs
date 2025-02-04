using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Type
{
    Fire,
    Stone,
    Basic,
    Plant
}

public class PlayerArmature : MonoBehaviour
{
    [SerializeField] private Type _type;
    [SerializeField] private Transform _projectileSpawnPoint;

    public Animator animator;

    private RaycastPerformer _performer;
    [Range(0, 1)][SerializeField] private float _ikWeight;

    public Type Type => _type;
    public Transform ProjectileSpawnPoint => _projectileSpawnPoint;

    private void Awake()
    {
        if (transform.parent != null)
        {
            _performer = transform.parent.GetComponent<RaycastPerformer>();
        }
    }

    void OnAnimatorIK(int layerIndex)
    {
        if (animator)
        {
            if (_performer.PerformRaycast(out RaycastHit raycastHit))
            {
                float maxDistance = 1.5f;
                float minDistance = 1f;

                Vector3 direction = raycastHit.point - animator.GetBoneTransform(HumanBodyBones.RightHand).position;
                float distance = direction.magnitude;

                if (distance > maxDistance)
                {
                    direction = direction.normalized * maxDistance;
                    raycastHit.point = animator.GetBoneTransform(HumanBodyBones.RightHand).position + direction;
                }
                else if(distance < minDistance)
                {
                    raycastHit.point += raycastHit.normal * (minDistance - distance);
                }

                animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _ikWeight);
                animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _ikWeight);
                animator.SetIKPosition(AvatarIKGoal.RightHand, raycastHit.point);
            }
        }
    }
}
