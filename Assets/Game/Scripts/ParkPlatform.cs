using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParkPlatform : SignalBinder, IInteractable
{
    [Header("Variables SO")]
    [SerializeField] private IntVariable VAR_CapturedCreatures;

    [Header("Settings")]
    public bool isOccupied = false;

    [System.Serializable]
    public struct MonsterModelMap
    {
        public StringVariable data;
        public GameObject model;
    }

    [Header("Models Mapping")]
    public List<MonsterModelMap> monsterModels;

    void Start()
    {
        // По умолчанию все модели выключены, если не занято
        if (!isOccupied)
        {
            foreach (var map in monsterModels) if (map.model != null) map.model.SetActive(false);
        }
        
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

            // Если ставим из инвентаря ("мешка"), мы не знаем какой монстр, 
            // поэтому включаем первую модель в списке как дефолтную
            if (monsterModels.Count > 0 && monsterModels[0].model != null)
            {
                monsterModels[0].model.SetActive(true);
            }

            Debug.Log("Монстр размещен на платформе!");
        }
        else
        {
            Debug.Log("У вас в мешке нет монстров! Поймайте их ночью.");
        }
    }

    public void PlaceMonsterDirectly(StringVariable data)
    {
        if (isOccupied) return;
        isOccupied = true;
        
        string targetValue = (data != null) ? data.Value : "";
        string targetName = (data != null) ? data.name : "NULL";

        Debug.Log($"<color=orange>[PLATFORM]</color> Доставка монстра. Ассет: <b>{targetName}</b>, Значение: <b>'{targetValue}'</b>");

        bool found = false;

        foreach (var map in monsterModels)
        {
            if (map.model == null) continue;

            string entryValue = (map.data != null) ? map.data.Value : "";
            string entryName = (map.data != null) ? map.data.name : "NULL";

            // 1. Пробуем сравнить по ссылке на ассет (самый надежный способ)
            bool matchByRef = (data != null && map.data == data);
            // 2. Пробуем сравнить по значению строки (если ассеты разные, но текст один)
            bool matchByVal = (!string.IsNullOrEmpty(targetValue) && targetValue == entryValue);

            if (matchByRef || matchByVal)
            {
                map.model.SetActive(true);
                found = true;
                Debug.Log($"<color=green>[PLATFORM]</color> Совпадение найдено! Активирована модель: <b>{map.model.name}</b> (по {(matchByRef ? "ссылке" : "значению")})");
            }
            else
            {
                map.model.SetActive(false);
            }
        }

        if (!found)
        {
            Debug.LogWarning($"<color=red>[PLATFORM]</color> Модель для монстра <b>{targetName} ('{targetValue}')</b> не найдена в списке Monster Models! Включаю первую по умолчанию.");
            if (monsterModels.Count > 0 && monsterModels[0].model != null)
            {
                monsterModels[0].model.SetActive(true);
            }
        }
        
        if (ParkManager.instance != null)
        {
            ParkManager.instance.RegisterPlatform(this);
        }
    }
}