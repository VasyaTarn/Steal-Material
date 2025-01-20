using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuLightController : MonoBehaviour
{
    [SerializeField] private Light _menuLight;
    [SerializeField] private float _duration;

    private Coroutine _currentCoroutine;


    private void Start()
    {
        EnableLight();
        
    }
    public void EnableLight()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(ChangeIntensity(0, 500, _duration));
    }

    public void DisableLight()
    {
        if (_currentCoroutine != null)
        {
            StopCoroutine(_currentCoroutine);
        }

        _currentCoroutine = StartCoroutine(ChangeIntensity(500, 0, _duration));
    }

    private IEnumerator ChangeIntensity(float startIntensity, float endIntensity, float time)
    {
        float elapsedTime = 0f;

        _menuLight.intensity = startIntensity;

        while (elapsedTime < time)
        {
            _menuLight.intensity = Mathf.Lerp(startIntensity, endIntensity, elapsedTime / time);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        _menuLight.intensity = endIntensity;
    }
}
