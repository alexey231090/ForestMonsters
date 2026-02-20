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

    public bool TryDeliverMonster()
    {
        // Логика доставки монстра в парк. Пока просто true
        // В будущем тут можно проверить, есть ли свободные места
        return true;
    }
}
