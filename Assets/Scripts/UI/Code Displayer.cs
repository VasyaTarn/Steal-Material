using TMPro;
using UnityEngine;

public class CodeDisplayer : MonoBehaviour
{
    private static TMP_Text codeText;
    private void Start()
    {
        codeText = GetComponent<TMP_Text>();
    }
    public static void displayCode(string code)
    {
        codeText.text = code;
    }
}
