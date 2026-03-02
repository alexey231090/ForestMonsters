using UnityEngine;
using System.Collections.Generic;
using Game.Interfaces;

public class MonsterGhostHandler : MonoBehaviour, IGhostable
{
    [SerializeField] private GhostSettings settings;
    
    private List<Renderer> renderers = new List<Renderer>();
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private bool isGhost = false;

    private void Awake()
    {
        // Находим все рендереры у монстра и его детей
        GetComponentsInChildren(true, renderers);
    }

    public void SetGhostMode(bool active)
    {
        if (isGhost == active || settings == null || settings.ghostMaterial == null) return;

        isGhost = active;

        if (active)
        {
            ApplyGhostEffect();
        }
        else
        {
            RestoreOriginalMaterials();
        }
    }

    private void ApplyGhostEffect()
    {
        originalMaterials.Clear();

        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            // Сохраняем оригинальные материалы
            originalMaterials[rend] = rend.sharedMaterials;

            // Создаем массив с материалом призрака того же размера, что и оригинальный
            Material[] ghostMats = new Material[rend.sharedMaterials.Length];
            for (int i = 0; i < ghostMats.Length; i++)
            {
                ghostMats[i] = settings.ghostMaterial;
            }

            rend.materials = ghostMats;
        }
    }

    private void RestoreOriginalMaterials()
    {
        foreach (var rend in renderers)
        {
            if (rend == null) continue;

            if (originalMaterials.TryGetValue(rend, out var mats))
            {
                rend.materials = mats;
            }
        }
    }
}
