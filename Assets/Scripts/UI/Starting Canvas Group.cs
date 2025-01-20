using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartingCanvasGroup : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;


    private void Awake()
    {
        _canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        Show();
    }

    public void Show()
    {
        _canvasGroup.DOFade(1, 1.5f);
    }
}
