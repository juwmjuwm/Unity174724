using System.Collections.Generic;
using UnityEngine;

public class PaintingColorManager : MonoBehaviour
{
    [Tooltip("Drag only the 12 child cubes here.")]
    public List<MeshRenderer> cubes = new List<MeshRenderer>(12);

    [Tooltip("The color for each corresponding cube.")]
    public List<Color> cubeColors = new List<Color>(12);

    private MaterialPropertyBlock propBlock;

    private void Awake()
    {
        ApplyColors();
    }

    // Previews color changes immediately in the Editor and Prefab Mode
    private void OnValidate()
    {
        ApplyColors();
    }

    // Applies the list of colors to the 12 cubes
    public void ApplyColors()
    {
        if (propBlock == null) propBlock = new MaterialPropertyBlock();

        int count = Mathf.Min(cubes.Count, cubeColors.Count);
        for (int i = 0; i < count; i++)
        {
            if (cubes[i] == null) continue;

            cubes[i].GetPropertyBlock(propBlock);
            propBlock.SetColor("_BaseColor", cubeColors[i]); // URP / HDRP
            propBlock.SetColor("_Color", cubeColors[i]);     // Built-in Render Pipeline
            cubes[i].SetPropertyBlock(propBlock);
        }
    }

    // Right-click the component header in the Inspector to populate the list from existing materials
    [ContextMenu("Read Colors From Cubes")]
    public void ReadColorsFromCubes()
    {
        cubeColors.Clear();
        foreach (MeshRenderer cube in cubes)
        {
            if (cube != null && cube.sharedMaterial != null)
                cubeColors.Add(cube.sharedMaterial.color);
            else
                cubeColors.Add(Color.white);
        }
    }
}
