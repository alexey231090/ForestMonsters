using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("--- Tycoon Economy ---")]
    public float money = 100f;
    public int capturedCreatures = 0;
    public float pricePerMeme = 1.5f;

    [Header("--- Items Inventory ---")]
    public int trapsCount = 12;
    public int camerasCount = 10;
    public float trapPrice = 20f;
    public float cameraPrice = 15f;

    [Header("--- Park ---")]
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();

    [Header("--- Spawners ---")]
    public VisitorSpawner visitorSpawner;
    public EnemySpawner enemySpawner;

    [Header("--- Time & Lighting ---")]
    public SunMovementController sunController; // Ссылка на контроллер солнца
    [Tooltip("Длительность дня в минутах")]
    public float dayDurationMinutes = 1f;
    [Tooltip("Длительность ночи в минутах")]
    public float nightDurationMinutes = 1f;

    [Header("--- State (Read Only) ---")]
    public bool isNight = false;
    [SerializeField] private float currentPhaseTimer = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // Автопоиск солнца, если забыл привязать
        if (sunController == null)
            sunController = Object.FindAnyObjectByType<SunMovementController>();

        StartDay();
    }

    void Update()
    {
        // Чит на деньги
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"$$$ Баланс: {money} | Ловушек: {trapsCount} | Камер: {camerasCount}");
        }

        // --- ЛОГИКА ВРЕМЕНИ ---
        HandleTimeCycle();
    }

    void HandleTimeCycle()
    {
        currentPhaseTimer += Time.deltaTime;

        float currentDuration = isNight ? nightDurationMinutes : dayDurationMinutes;
        float durationInSeconds = currentDuration * 60f;

        // Рассчитываем прогресс от 0.0 до 1.0 (0% -> 100% фазы)
        float progress = Mathf.Clamp01(currentPhaseTimer / durationInSeconds);

        // Обновляем визуальное положение солнца через контроллер
        if (sunController != null)
        {
            sunController.UpdateSunPosition(progress, isNight);
        }

        // Если время вышло — меняем фазу
        if (currentPhaseTimer >= durationInSeconds)
        {
            SkipCurrentPhase();
        }
    }

    // --- СМЕНА ФАЗ ---

    public void StartDay()
    {
        isNight = false;
        currentPhaseTimer = 0f;

        if (sunController) sunController.SetVisualsForDay();

        if (enemySpawner != null) enemySpawner.ClearEnemies();
        if (visitorSpawner != null) visitorSpawner.StartNewDay();

        Debug.Log(">>> НАСТУПИЛ ДЕНЬ");
    }

    public void StartNight()
    {
        isNight = true;
        currentPhaseTimer = 0f;

        if (sunController) sunController.SetVisualsForNight();

        if (visitorSpawner != null) visitorSpawner.StopSpawning();
        if (enemySpawner != null) enemySpawner.SpawnEnemies();

        Debug.Log(">>> НАСТУПИЛА НОЧЬ");
    }

    public void SkipCurrentPhase()
    {
        if (isNight) StartDay();
        else StartNight();
    }

    // --- ЭКОНОМИКА И ИНВЕНТАРЬ ---

    public bool BuyTrap()
    {
        if (money >= trapPrice) { money -= trapPrice; trapsCount++; return true; }
        return false;
    }

    public bool BuyCamera()
    {
        if (money >= cameraPrice) { money -= cameraPrice; camerasCount++; return true; }
        return false;
    }

    public bool TryUseTrap()
    {
        if (trapsCount > 0) { trapsCount--; return true; }
        return false;
    }

    public bool TryUseCamera()
    {
        if (camerasCount > 0) { camerasCount--; return true; }
        return false;
    }

    public void AddCreature()
    {
        capturedCreatures++;
    }

    public bool TryRemoveCreature()
    {
        if (capturedCreatures > 0) { capturedCreatures--; return true; }
        return false;
    }

    public void AddMoney(float amount)
    {
        money += amount;
    }
}