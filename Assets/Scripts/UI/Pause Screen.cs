using UnityEngine;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    private Image _pauseBackground;
    [SerializeField] private GameObject _mouseSensitivity;

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
            _mouseSensitivity.SetActive(!_mouseSensitivity.activeSelf);

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

        if (Input.GetKeyDown(KeyCode.F1) && _pauseBackground.enabled)
        {
            _pauseBackground.enabled = false;
            _mouseSensitivity.SetActive(false);

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            isPause = false;
        }
    }

    public void SetDarkBackground()
    {
        Color color = _pauseBackground.color;
        color.a = Mathf.Clamp01(1f);
        _pauseBackground.color = color;
    }
}
