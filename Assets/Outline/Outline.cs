using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Outline : MonoBehaviour
{
    private Renderer[] renderers;

    public Material outlineMaterial;

    private Material[] withOutline;
    private Material[] withoutOutline;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials.ToList();

            withoutOutline = materials.ToArray();

            materials.Add(outlineMaterial);

            withOutline = materials.ToArray();
        }
    }

    public void enable()
    {
        foreach (var renderer in renderers)
        {
            renderer.materials = withOutline;
        }
    }

    public void disable()
    {
        foreach (var renderer in renderers)
        {
            renderer.materials = withoutOutline;
        }
    }
}
