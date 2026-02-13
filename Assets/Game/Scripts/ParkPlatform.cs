using UnityEngine;
using System.Collections;

public class ParkPlatform : MonoBehaviour
{
    [Header("Settings")]
    public GameObject monsterModel;
    public bool isOccupied = false;

    void Start()
    {
        if (monsterModel != null) monsterModel.SetActive(isOccupied);
    }

    public void TryPlaceMonster()
    {
        if (isOccupied)
        {
            Debug.Log("Эта платформа уже занята!");
            return;
        }

        // 2. Проверяем, есть ли монстры в инвентаре (через GameManager)
        if (GameManager.instance != null && GameManager.instance.TryRemoveCreature())
        {
            isOccupied = true;

            if (monsterModel != null)
            {
                monsterModel.SetActive(true);
            }

            // Добавляем эту платформу в список активных, чтобы посетители её видели
            GameManager.instance.activePlatforms.Add(this);

            Debug.Log("Монстр размещен на платформе!");
        }
        else
        {
            Debug.Log("У вас в мешке нет монстров! Поймайте их ночью.");
        }
    }

    public void PlaceMonsterDirectly()
    {
        if (isOccupied) return;

        isOccupied = true;
        if (monsterModel != null) monsterModel.SetActive(true);
        
        if (GameManager.instance != null)
        {
            GameManager.instance.activePlatforms.Add(this);
        }
    }
}