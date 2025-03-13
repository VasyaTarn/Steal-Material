using DG.Tweening;
using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [SerializeField] private RectTransform[] images;

    private float targetHeight = 40f;
    private float duration = 0.05f; 

    private Vector2[] originalSizes;

    void Start()
    {
        originalSizes = new Vector2[images.Length];

        for (int i = 0; i < images.Length; i++)
        {
            originalSizes[i] = images[i].sizeDelta;
        }
    }

    public void AnimateDamageResize()
    {
        for (int i = 0; i < images.Length; i++)
        {
            var img = images[i];
            img.DOKill();

            int index = i;

            img.DOSizeDelta(new Vector2(img.sizeDelta.x, targetHeight), duration)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (index >= 0 && index < originalSizes.Length)
                    {
                        img.sizeDelta = originalSizes[index];
                    }
                });
        }
    }
}
