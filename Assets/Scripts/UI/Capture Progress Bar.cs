using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CaptureProgressBar : MonoBehaviour
{
    private Camera _playerCamera;
    [SerializeField] private Image _progressBarImage;
    [SerializeField] private GameObject _lockImage;

    public Image ProgressBarImage => _progressBarImage;
    public GameObject LockImage => _lockImage;

    private void Update()
    {
        if (_playerCamera == null && Camera.main != null)
        {
            _playerCamera = Camera.main;
        }

        if(_playerCamera != null)
        {
            Vector3 direction = transform.position - _playerCamera.transform.position;
            direction.y = 0;

            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
