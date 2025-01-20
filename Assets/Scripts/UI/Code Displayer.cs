using TMPro;
using UnityEngine;

public class CodeDisplayer : MonoBehaviour
{
    private static TMP_Text _codeText;

    private void Start()
    {
        _codeText = GetComponent<TMP_Text>();
    }
    public static void displayCode(string code)
    {
        _codeText.text = code;
    }
}
