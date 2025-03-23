using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MouseSensitivityController : MonoBehaviour
{
    private float minSensitivity = 0.1f;
    private float maxSensitivity = 10.0f;
    private float defaultSensitivity = 2.0f;

    private PlayerMovementController _playerMovementController;


    private void Start()
    {
        _playerMovementController = GetComponent<PlayerMovementController>();

        UIReferencesManager.Instance.SensitivitySlider.minValue = minSensitivity;
        UIReferencesManager.Instance.SensitivitySlider.maxValue = maxSensitivity;

        UIReferencesManager.Instance.SensitivitySlider.value = defaultSensitivity;

        UIReferencesManager.Instance.SensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        UpdateSensitivityText(defaultSensitivity);

        ApplySensitivity(defaultSensitivity);
    }

    private void OnSensitivityChanged(float newValue)
    {
        UpdateSensitivityText(newValue);
        ApplySensitivity(newValue);
    }

    private void UpdateSensitivityText(float value)
    {
        UIReferencesManager.Instance.SensitivityValueText.text = value.ToString("F1", CultureInfo.InvariantCulture);
    }

    private void ApplySensitivity(float value)
    {
        if (_playerMovementController != null)
        {
            _playerMovementController.SetSensitivity(value);
        }
    }

    private void OnDestroy()
    {
        UIReferencesManager.Instance.SensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);
    }
}
