using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    private Image _pauseBackground;

    public static bool isPause;


    private void Start()
    {
        _pauseBackground = GetComponent<Image>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _pauseBackground.enabled = !_pauseBackground.enabled;

            if(_pauseBackground.enabled)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                isPause = true;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                isPause = false;
            }
        }
    }

    public void SetDarkBackground()
    {
        Color color = _pauseBackground.color;
        color.a = Mathf.Clamp01(1f);
        _pauseBackground.color = color;
    }
}
