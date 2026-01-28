using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Tycoon Economy")]
    public float money = 100f;        // Money
    public int capturedCreatures = 0; // Captured creatures
    public float pricePerMeme = 1.5f;

    [Header("Items Inventory")]
    public int trapsCount = 12;   
    public int camerasCount = 10; 
    public float trapPrice = 20f;
    public float cameraPrice = 15f;

    //   
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();

    [Header("Spawners")]
    public VisitorSpawner visitorSpawner; // Spawner for visitor creatures
    public EnemySpawner enemySpawner;     //     ()

    [Header("Time Settings")]
    public float dayDurationMinutes = 1f;
    public float nightDurationMinutes = 1f;

    [Header("Lighting Controller")]
    public SunMovementController sunController;

    [Header("State (Read Only)")]
    public bool isNight = false;
    public float currentPhaseTimer = 0f;
    
    private float phaseChangeProtectionTimer = 0f;
    private const float PHASE_CHANGE_PROTECTION_DURATION = 0.5f; // 0.5 seconds protection

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (sunController == null)
        {
            sunController = Object.FindAnyObjectByType<SunMovementController>();
        }
        StartDay();
        if (enemySpawner == null) 
        {
            Debug.Log(" enimySpavner.cs  GameManager");
        }
    }

    void Update()
    {
            
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"$$$ : {money} | : {trapsCount} | : {camerasCount}");
        }

        // Синхронизируем переменную isNight с реальным положением солнца
        // Защита от смены фазы сразу после вызова SkipCurrentPhase
        if (sunController != null && phaseChangeProtectionTimer <= 0)
        {
            // Определяем текущую фазу на основе ротации солнца
            bool currentlyDay = sunController.IsCurrentlyDay();
            // Обновляем isNight (противоположно isDay)
            bool oldIsNight = isNight;
            isNight = !currentlyDay;
            
            if (oldIsNight != isNight)
            {
                // Фаза изменилась, вызываем соответствующий метод
                if (isNight)
                {
                    StartNight();
                }
                else
                {
                    StartDay();
                }
            }
        }
        else if (phaseChangeProtectionTimer > 0)
        {
            phaseChangeProtectionTimer -= Time.deltaTime;
        }

        currentPhaseTimer += Time.deltaTime;
 
        
        float durationSec = (!isNight) ? dayDurationMinutes * 60f : nightDurationMinutes * 60f;

        if (sunController && sunController.sunLight && currentPhaseTimer <= durationSec)
        {
            float progress = currentPhaseTimer / durationSec;


            sunController.sunLight.intensity = (!isNight) ? 1f : 0.1f;
        }
         
        
        if (currentPhaseTimer >= durationSec)
        {
            if (isNight) StartDay();
            else StartNight();
        }
    }

    // ---    ---

    public bool BuyTrap()
    {
        if (money >= trapPrice)
        {
            money -= trapPrice;
            trapsCount++;
            Debug.Log(" !");
            return true;
        }
        Debug.Log("  !");
        return false;
    }

    public bool BuyCamera()
    {
        if (money >= cameraPrice)
        {
            money -= cameraPrice;
            camerasCount++;
            Debug.Log("Buy camera");
            return true;
        }
        Debug.Log("No money");
        return false;
    }

    
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

    
    
    public void AddCreature()
    {
        capturedCreatures++;
        Debug.Log($"[]  !  : {capturedCreatures}");
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
        Debug.Log($"+++ : +{amount}. : {money}");
    }

    // ---   ---
    public void StartDay()
    {
        Debug.Log($"StartDay called. Previous isNight: {isNight}");
        isNight = false;
        // Не сбрасываем таймер, чтобы не нарушать естественную смену дня/ночи
        // currentPhaseTimer = 0f;
        if (sunController)
        {
            RenderSettings.fogColor = sunController.dayFog;
            sunController.SetDayPhase(true); // isDay = true (день)
            // Убедимся, что солнце находится в правильной стартовой позиции
            sunController.transform.rotation = Quaternion.Euler(sunController.dayStartRotation);
        }
        RenderSettings.ambientIntensity = 1f;

        //
        if (enemySpawner != null) enemySpawner.ClearEnemies();
        if (visitorSpawner != null) visitorSpawner.StartNewDay();

        Debug.Log(">>> DAY STARTED (natural)");
        
        // Активируем защиту от немедленной смены фазы
        phaseChangeProtectionTimer = PHASE_CHANGE_PROTECTION_DURATION;
    }

    public void StartNight()
    {
        Debug.Log($"StartNight called. Previous isNight: {isNight}");
        isNight = true;
        // Не сбрасываем таймер, чтобы не нарушать естественную смену дня/ночи
        // currentPhaseTimer = 0f;
        if (sunController)
        {
            RenderSettings.fogColor = sunController.nightFog;
            sunController.SetDayPhase(false); // isDay = false (ночь)
            // Убедимся, что солнце находится в правильной стартовой позиции
            sunController.transform.rotation = Quaternion.Euler(sunController.nightEndRotation);
        }
        RenderSettings.ambientIntensity = 0.2f;

        // Останавливаем дневных спавнеров и запускаем ночных
        if (visitorSpawner != null) visitorSpawner.StopSpawning();
        if (enemySpawner != null) enemySpawner.SpawnEnemies();

        Debug.Log(">>> NIGHT STARTED (natural)");
        
        // Активируем защиту от немедленной смены фазы
        phaseChangeProtectionTimer = PHASE_CHANGE_PROTECTION_DURATION;
    }

    // Метод для телепортации солнца в заданную фазу
    public void TeleportSunToPhase(bool toDayPhase)
    {
        if (sunController != null)
        {
            sunController.SetDayPhase(toDayPhase);
            if (toDayPhase)
            {
                sunController.transform.rotation = Quaternion.Euler(sunController.dayStartRotation);
            }
            else
            {
                sunController.transform.rotation = Quaternion.Euler(sunController.nightEndRotation);
            }
        }
    }

    public void SkipCurrentPhase()
    {
        Debug.Log($"SkipCurrentPhase called. Current isNight: {isNight}");
        
        if (isNight)
        {
            // Если сейчас ночь, переходим к дню
            StartDay();
        }
        else
        {
            // Если сейчас день, переходим к ночи
            StartNight();
        }
        
        // Активируем защиту от немедленной смены фазы
        phaseChangeProtectionTimer = PHASE_CHANGE_PROTECTION_DURATION;
        
        Debug.Log($"SkipCurrentPhase finished. New isNight: {isNight}");
    }

    public void SkipToNight()
    {
        if (sunController == null)
        {
            sunController = Object.FindAnyObjectByType<SunMovementController>();
        }
        if (sunController != null)
        {
            sunController.InstantTransitionToNight();
        }
        StartNight();
    }
}


