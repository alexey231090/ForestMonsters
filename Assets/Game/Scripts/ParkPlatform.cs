using UnityEngine;
using System.Collections;

public class ParkPlatform : MonoBehaviour, IInteractable
{
    [Header("Variables SO")]
    [SerializeField] private IntVariable VAR_CapturedCreatures;

    [Header("Settings")]
    public GameObject monsterModel;
    public bool isOccupied = false;

    void Start()
    {
        if (monsterModel != null) monsterModel.SetActive(isOccupied);
        
        // Регистрируем платформу в любом случае, чтобы менеджер знал о свободном месте
        if (ParkManager.instance != null) 
            ParkManager.instance.RegisterPlatform(this);
    }

    void OnDestroy()
    {
        if (ParkManager.instance != null) ParkManager.instance.UnregisterPlatform(this);
    }

    public void Interact() => TryPlaceMonster();

    public void TryPlaceMonster()
    {
        if (isOccupied)
        {
            Debug.Log("Эта платформа уже занята!");
            return;
        }

        // 2. Проверяем, есть ли монстры в инвентаре
        if (VAR_CapturedCreatures != null && VAR_CapturedCreatures.Value > 0)
        {
            VAR_CapturedCreatures.ApplyChange(-1);
            isOccupied = true;

            if (monsterModel != null)
            {
                monsterModel.SetActive(true);
            }

            // Добавляем эту платформу в список активных
            if (ParkManager.instance != null) ParkManager.instance.RegisterPlatform(this);

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
        
        if (ParkManager.instance != null)
        {
            ParkManager.instance.RegisterPlatform(this);
        }
    }
}