using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class OutlineCustom : MonoBehaviour
{
    private Renderer[] _renderers;

    public Material outlineMaterial;

    private Material[] _withOutline;
    private Material[] _withoutOutline;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in _renderers)
        {
            var materials = renderer.sharedMaterials.ToList();

            _withoutOutline = materials.ToArray();

            materials.Add(outlineMaterial);

            _withOutline = materials.ToArray();
        }
    }

    public void Enable()
    {
        foreach (var renderer in _renderers)
        {
            renderer.materials = _withOutline;
        }
    }

    public void Disable()
    {
        foreach (var renderer in _renderers)
        {
            renderer.materials = _withoutOutline;
        }
    }
}
