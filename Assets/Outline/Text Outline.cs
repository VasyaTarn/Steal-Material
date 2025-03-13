using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TextOutline : MonoBehaviour
{
    private void Start()
    {
        TMP_Text text = GetComponent<TMP_Text>();

        text.fontMaterial = new Material(text.fontMaterial);

        text.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.3f);
        text.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
    }
}
