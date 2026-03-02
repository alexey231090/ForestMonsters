using UnityEngine;
using System.Collections.Generic;

public class ParkManager : MonoBehaviour
{
    public static ParkManager instance;

    [Header("Settings Assets")]
    [SerializeField] private FloatVariable VAR_Money;

    [Header("Park State")]
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();
    public float pricePerMeme = 1.5f;

    void Awake()
    {
        instance = this;
    }

    public void RegisterPlatform(ParkPlatform platform)
    {
        if (!activePlatforms.Contains(platform))
        {
            activePlatforms.Add(platform);
        }
    }

    public void UnregisterPlatform(ParkPlatform platform)
    {
        if (activePlatforms.Contains(platform))
        {
            activePlatforms.Remove(platform);
        }
    }

    public bool TryDeliverMonster(StringVariable monsterData)
    {
        // Ищем первую свободную платформу
        foreach (var platform in activePlatforms)
        {
            if (platform != null && !platform.isOccupied)
            {
                platform.PlaceMonsterDirectly(monsterData);
                Debug.Log($"[PARK] Monster {monsterData?.name} placed on platform: {platform.name}");
                return true;
            }
        }

        Debug.LogWarning("[PARK] No free platforms available!");
        return false;
    }
}
