using UnityEngine;
using System.Collections.Generic;

public class ParkManager : MonoBehaviour
{
    public static ParkManager instance;
    
    [Header("Settings")]
    public string parkTriggerTag = "ParkTrigger";
    
    [Header("References")]
    public List<ParkPlatform> platforms = new List<ParkPlatform>();

    void Awake()
    {
        instance = this;
        // Если платформы не назначены вручную, попробуем найти их в детях
        if (platforms.Count == 0)
        {
            platforms.AddRange(GetComponentsInChildren<ParkPlatform>());
        }
    }

    public bool TryDeliverMonster()
    {
        foreach (var platform in platforms)
        {
            if (platform != null && !platform.isOccupied)
            {
                platform.PlaceMonsterDirectly();
                Debug.Log($"[PARK] Monster delivered to platform: {platform.name}");
                return true;
            }
        }

        Debug.LogWarning("[PARK] No free platforms available!");
        return false;
    }
}
