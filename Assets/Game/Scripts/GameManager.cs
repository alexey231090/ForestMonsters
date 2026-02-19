using UnityEngine;
using System.Collections.Generic;

public class GameManager : SignalBinder
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

    [Header("--- Event Listeners (Inputs) ---")]
    [SerializeField] private GameEvent GET_onDayStarted;
    [SerializeField] private GameEvent GET_onNightStarted;

    [Header("--- State (Read Only) ---")]
    public bool isNight = false;

    void Awake()
    {
        instance = this;

        // Подписываем логику на события времени
        Bind(GET_onDayStarted, OnDayStarted);
        Bind(GET_onNightStarted, OnNightStarted);
    }

    // Наследуемся от SignalBinder, поэтому переопределяем методы, если нужно,
    // но в SignalBinder OnEnable/OnDisable делают основную работу.
    // Если здесь будут свои OnEnable/OnDisable, нужно не забывать base.OnEnable().

    void Update()
    {
        // Чит на деньги
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"$$$ Баланс: {money} | Ловушек: {trapsCount} | Камер: {camerasCount}");
        }
    }

    // --- ОБРАБОТЧИКИ СОБЫТИЙ ВРЕМЕНИ (через SignalBinder) ---

    private void OnDayStarted()
    {
        isNight = false;
        
        if (enemySpawner != null) enemySpawner.ClearEnemies();
        if (visitorSpawner != null) visitorSpawner.StartNewDay();

        Debug.Log(">>> GameManager: Реагирую на НАЧАЛО ДНЯ");
    }

    private void OnNightStarted()
    {
        isNight = true;

        if (visitorSpawner != null) visitorSpawner.StopSpawning();
        if (enemySpawner != null) enemySpawner.SpawnEnemies();

        Debug.Log(">>> GameManager: Реагирую на НАЧАЛО НОЧИ");
    }

    // Чит или кнопка пропуска — теперь должна идти через SunMovementController,
    // либо GameManager может найти его и вызвать TogglePhase.
    public void SkipCurrentPhase()
    {
        var sun = Object.FindAnyObjectByType<SunMovementController>();
        if (sun != null) sun.TogglePhase();
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