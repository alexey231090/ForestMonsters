using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Tycoon Economy")]
    public float money = 100f;        // Деньги
    public int capturedCreatures = 0; // Инвентарь мемов
    public float pricePerMeme = 1.5f;

    [Header("Items Inventory")]
    public int trapsCount = 2;   // Количество ловушек
    public int camerasCount = 1; // Количество камер
    public float trapPrice = 20f;
    public float cameraPrice = 15f;

    // Список активных платформ в парке
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();

    [Header("Spawners")]
    public VisitorSpawner visitorSpawner; // Ссылка на спавнер людей
    public EnemySpawner enemySpawner;     // Ссылка на спавнер врагов (НОВОЕ)

    [Header("Time Settings")]
    public float dayDurationMinutes = 1f;
    public float nightDurationMinutes = 1f;

    [Header("Lighting")]
    public Light sunLight;
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    [Header("State (Read Only)")]
    public bool isNight = false;
    public float currentPhaseTimer = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartDay();
        if (enemySpawner == null)
        {
            Debug.Log("Нет enimySpavner.cs в GameManager");
        }
    }

    void Update()
    {
        // Чит на проверку баланса
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"$$$ Баланс: {money} | Ловушек: {trapsCount} | Камер: {camerasCount}");
        }

        currentPhaseTimer += Time.deltaTime;

        if (!isNight) // ДЕНЬ
        {
            float dayDurationSec = dayDurationMinutes * 60f;
            if (sunLight)
            {
                float progress = currentPhaseTimer / dayDurationSec;
                float angle = Mathf.Lerp(0f, 180f, progress);
                sunLight.transform.rotation = Quaternion.Euler(angle, 0, 0);
                sunLight.intensity = 1f;
            }
            if (currentPhaseTimer >= dayDurationSec) StartNight();
        }
        else // НОЧЬ
        {
            float nightDurationSec = nightDurationMinutes * 60f;
            if (sunLight)
            {
                float progress = currentPhaseTimer / nightDurationSec;
                float angle = Mathf.Lerp(180f, 360f, progress);
                sunLight.transform.rotation = Quaternion.Euler(angle, 0, 0);
                sunLight.intensity = 0.1f;
            }
            if (currentPhaseTimer >= nightDurationSec) StartDay();
        }
    }

    // --- МАГАЗИН И ПРЕДМЕТЫ ---

    public bool BuyTrap()
    {
        if (money >= trapPrice)
        {
            money -= trapPrice;
            trapsCount++;
            Debug.Log("Куплена ловушка!");
            return true;
        }
        Debug.Log("Не хватает денег на ловушку!");
        return false;
    }

    public bool BuyCamera()
    {
        if (money >= cameraPrice)
        {
            money -= cameraPrice;
            camerasCount++;
            Debug.Log("Куплена камера!");
            return true;
        }
        Debug.Log("Не хватает денег на камеру!");
        return false;
    }

    // Методы для использования при строительстве
    public bool TryUseTrap()
    {
        if (trapsCount > 0)
        {
            trapsCount--;
            return true;
        }
        return false;
    }

    public bool TryUseCamera()
    {
        if (camerasCount > 0)
        {
            camerasCount--;
            return true;
        }
        return false;
    }

    // --- ЭКОНОМИКА МЕМОВ ---
    public void AddCreature()
    {
        capturedCreatures++;
        Debug.Log($"[Инвентарь] Мем пойман! В мешке: {capturedCreatures}");
    }

    public bool TryRemoveCreature()
    {
        if (capturedCreatures > 0)
        {
            capturedCreatures--;
            return true;
        }
        return false;
    }

    public void AddMoney(float amount)
    {
        money += amount;
        Debug.Log($"+++ ПРИБЫЛЬ: +{amount}. Итого: {money}");
    }

    // --- СМЕНА ФАЗ ---
    public void StartDay()
    {
        isNight = false;
        currentPhaseTimer = 0f;
        RenderSettings.fogColor = dayFog;
        RenderSettings.ambientIntensity = 1f;

        // Обращаемся к внешним скриптам
        if (enemySpawner != null) enemySpawner.ClearEnemies();
        if (visitorSpawner != null) visitorSpawner.StartNewDay();

        Debug.Log(">>> ДЕНЬ (Парк открыт)");
    }

    public void StartNight()
    {
        isNight = true;
        currentPhaseTimer = 0f;
        RenderSettings.fogColor = nightFog;
        RenderSettings.ambientIntensity = 0.2f;

        // Обращаемся к внешним скриптам
        if (visitorSpawner != null) visitorSpawner.StopSpawning();
        if (enemySpawner != null) enemySpawner.SpawnEnemies();

        Debug.Log(">>> НОЧЬ (Охота началась)");
    }

    public void SkipCurrentPhase()
    {
        if (isNight) StartDay();
        else StartNight();
    }
}