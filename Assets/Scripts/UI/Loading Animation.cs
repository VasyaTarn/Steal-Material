using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingAnimation : MonoBehaviour
{
    private float _rotationDuration = 1f;

    private Tween _rotationTween;

    private void OnEnable()
    {
        StartLoadingAnimation();
    }

    private void OnDisable()
    {
        StopLoadingAnimation();
    }

    private void StartLoadingAnimation()
    {
        _rotationTween = transform
            .DORotate(new Vector3(0, 0, -360), _rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1);
    }

    private void StopLoadingAnimation()
    {
        if(_rotationTween != null)
        {
            _rotationTween.Kill();
            _rotationTween = null;
        }
    }
}
