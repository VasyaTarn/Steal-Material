using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinUIView : MonoBehaviour
{
    [SerializeField] private TMP_InputField _inputCodeField;
    [SerializeField] private TMP_Text _codeText;
    [SerializeField] private Button _submit;
    [SerializeField] private Button _back;
    [SerializeField] private Image _loadingIcon;
    [SerializeField] private TMP_Text _errorText;
    [SerializeField] private Button _exit;
    [SerializeField] private Button _create;
    [SerializeField] private Button _join;


    public void DislayJoinUI()
    {
        _inputCodeField.gameObject.SetActive(true);
        _codeText.gameObject.SetActive(true);
        _submit.gameObject.SetActive(true);
        _back.gameObject.SetActive(true);
        _loadingIcon.gameObject.SetActive(false);
        _errorText.gameObject.SetActive(true);
        _exit.gameObject.SetActive(true);
    }

    public void HideStartUI()
    {
        _create.gameObject.SetActive(false);
        _join.gameObject.SetActive(false);
    }

    public TMP_Text GetErrorText()
    {
        return _errorText;
    }

    public void ResetErrorText()
    {
        _errorText.text = string.Empty;
    }
}
