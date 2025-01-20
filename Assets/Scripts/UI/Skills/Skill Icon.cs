using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class SkillIcon : MonoBehaviour
{
    [SerializeField] protected Image image;
    [SerializeField] protected TMP_Text text;
    [SerializeField] protected Image cdFiller;

    private Tween _cooldownTween;


    public void ActivateCooldown(float cd)
    {
        _cooldownTween?.Kill();

        cdFiller.gameObject.SetActive(true);

        _cooldownTween = cdFiller.DOFillAmount(0, cd)
            .SetEase(Ease.Linear)
            .OnUpdate(() => UpdateCooldownText(cdFiller.fillAmount * cd))
            .OnComplete(CooldownComplete);
    }

    private void UpdateCooldownText(float remainingTime)
    {
        text.text = Mathf.Ceil(remainingTime).ToString();
    }

    private void CooldownComplete()
    {
        image.color = Color.white;
        text.text = "";
        cdFiller.fillAmount = 0;

        if (cdFiller.gameObject.activeSelf)
        {
            cdFiller.gameObject.SetActive(false);
            cdFiller.fillAmount = 1;
        }

        _cooldownTween = null;
    }
}
