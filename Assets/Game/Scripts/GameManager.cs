using UnityEngine;
using System.Collections.Generic;

public class GameManager : SignalBinder
{
    /*
    public static GameManager instance;



    [Header("Subscribed Events")
    [SerializeField] private GameEvent GET_onDayStarted;
    [SerializeField] private GameEvent GET_onNightStarted;

    [Header("Variables SO")]
    [SerializeField] private FloatVariable VAR_Money;
    [SerializeField] private IntVariable VAR_CapturedCreatures;
    [SerializeField] private IntVariable VAR_TrapsCount;
    [SerializeField] private IntVariable VAR_CamerasCount;

    [Header("--- Tycoon Economy ---")]
    public float money { get => VAR_Money != null ? VAR_Money.Value : 0; set { if (VAR_Money != null) VAR_Money.Value = value; } }
    public int capturedCreatures { get => VAR_CapturedCreatures != null ? VAR_CapturedCreatures.Value : 0; set { if (VAR_CapturedCreatures != null) VAR_CapturedCreatures.Value = value; } }
    public float pricePerMeme = 1.5f;

    [Header("--- Items Inventory ---")]
    public int trapsCount { get => VAR_TrapsCount != null ? VAR_TrapsCount.Value : 0; set { if (VAR_TrapsCount != null) VAR_TrapsCount.Value = value; } }
    public int camerasCount { get => VAR_CamerasCount != null ? VAR_CamerasCount.Value : 0; set { if (VAR_CamerasCount != null) VAR_CamerasCount.Value = value; } }
    public float trapPrice = 20f;
    public float cameraPrice = 15f;

    [Header("--- Park --")]
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();

    void Awake()
    {
        instance = this;

        // Подписываем логику на события времени
        Bind(GET_onDayStarted, OnDayStarted);
        Bind(GET_onNightStarted, OnNightStarted);
    }

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
        Debug.Log(">>> GameManager: Реагирую на НАЧАЛО ДНЯ");
    }

    private void OnNightStarted()
    {
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
        if (VAR_Money != null && VAR_TrapsCount != null && VAR_Money.Value >= trapPrice) 
        { 
            VAR_Money.ApplyChange(-trapPrice); 
            VAR_TrapsCount.ApplyChange(1); 
            return true; 
        }
        return false;
    }

    public bool BuyCamera()
    {
        if (VAR_Money != null && VAR_CamerasCount != null && VAR_Money.Value >= cameraPrice) 
        { 
            VAR_Money.ApplyChange(-cameraPrice); 
            VAR_CamerasCount.ApplyChange(1); 
            return true; 
        }
        return false;
    }

    public bool TryUseTrap()
    {
        if (VAR_TrapsCount != null && VAR_TrapsCount.Value > 0) 
        { 
            VAR_TrapsCount.ApplyChange(-1); 
            return true; 
        }
        return false;
    }

    public bool TryUseCamera()
    {
        if (VAR_CamerasCount != null && VAR_CamerasCount.Value > 0) 
        { 
            VAR_CamerasCount.ApplyChange(-1); 
            return true; 
        }
        return false;
    }

    public void AddCreature()
    {
        if (VAR_CapturedCreatures != null) VAR_CapturedCreatures.ApplyChange(1);
    }

    public bool TryRemoveCreature()
    {
        if (VAR_CapturedCreatures != null && VAR_CapturedCreatures.Value > 0) 
        { 
            VAR_CapturedCreatures.ApplyChange(-1); 
            return true; 
        }
        return false;
    }

    public void AddMoney(float amount)
    {
        if (VAR_Money != null) VAR_Money.ApplyChange(amount);
    }
    */
}