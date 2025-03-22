using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class ObjectOutline : MonoBehaviour
{
    [Range(0.0f, 100.0f)]
    public float outlineWidth = 2.0f;
    public Color outlineColor = Color.yellow;
    public bool includeChildren = true;
    public bool activeOnStart = true;

    private List<Renderer> affectedRenderers = new List<Renderer>();
    private List<Material> outlineMaterials = new List<Material>();
    private Dictionary<Renderer, Material[]> originalMaterialsMap = new Dictionary<Renderer, Material[]>();
    private bool isOutlineActive = false;

    [HideInInspector]
    public Shader outlineShader;

    private void Awake()
    {
        outlineShader = Shader.Find("Custom/Outline");
        if (outlineShader == null)
        {
            enabled = false;
            return;
        }

        CollectRenderers();

        CreateOutlineMaterials();
    }

    private void Start()
    {
        if (activeOnStart)
        {
            ActivateOutline();
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying && isOutlineActive)
        {
            UpdateOutlineProperties();
        }
    }

    private void CollectRenderers()
    {
        affectedRenderers.Clear();
        originalMaterialsMap.Clear();

        if (includeChildren)
        {
            GetComponentsInChildren(affectedRenderers);
        }
        else
        {
            Renderer mainRenderer = GetComponent<Renderer>();
            if (mainRenderer != null)
            {
                affectedRenderers.Add(mainRenderer);
            }
        }

        foreach (Renderer renderer in affectedRenderers)
        {
            originalMaterialsMap[renderer] = renderer.materials;
        }
    }

    private void CreateOutlineMaterials()
    {
        foreach (Material material in outlineMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
        outlineMaterials.Clear();

        foreach (Renderer renderer in affectedRenderers)
        {
            foreach (Material originalMaterial in originalMaterialsMap[renderer])
            {
                Material outlineMaterial = new Material(outlineShader);
                outlineMaterial.SetFloat("_OutlineWidth", outlineWidth);
                outlineMaterial.SetColor("_OutlineColor", outlineColor);
                outlineMaterials.Add(outlineMaterial);
            }
        }
    }

    public void ActivateOutline()
    {
        if (isOutlineActive) return;

        int materialIndex = 0;
        foreach (Renderer renderer in affectedRenderers)
        {
            if (renderer == null) continue;

            Material[] originalMaterials = originalMaterialsMap[renderer];
            Material[] newMaterials = new Material[originalMaterials.Length * 2];

            for (int i = 0; i < originalMaterials.Length; i++)
            {
                newMaterials[i * 2] = originalMaterials[i];
                newMaterials[i * 2 + 1] = outlineMaterials[materialIndex++];
            }

            renderer.materials = newMaterials;
        }

        isOutlineActive = true;
    }

    public void DeactivateOutline()
    {
        if (!isOutlineActive) return;

        foreach (Renderer renderer in affectedRenderers)
        {
            if (renderer == null) continue;
            renderer.materials = originalMaterialsMap[renderer];
        }

        isOutlineActive = false;
    }

    public void UpdateOutlineProperties()
    {
        for (int i = 0; i < outlineMaterials.Count; i++)
        {
            if (outlineMaterials[i] != null)
            {
                outlineMaterials[i].SetFloat("_OutlineWidth", outlineWidth);
                outlineMaterials[i].SetColor("_OutlineColor", outlineColor);
            }
        }
    }

    private void OnEnable()
    {
        if (outlineShader != null && activeOnStart)
        {
            ActivateOutline();
        }
    }

    private void OnDisable()
    {
        DeactivateOutline();
    }

    private void OnDestroy()
    {
        foreach (Material material in outlineMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}