using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Tycoon Economy")]
    public float money = 100f; // Дадим немного денег на старт
    public int capturedCreatures = 0;
    public float pricePerMeme = 1.5f;

    [Header("Items Inventory (NEW)")]
    public int trapsCount = 2;   // Стартовое кол-во ловушек
    public int camerasCount = 1; // Стартовое кол-во камер
    public float trapPrice = 20f;
    public float cameraPrice = 50f;

    // Список активных платформ
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();

    [Header("Visitors")]
    public VisitorSpawner visitorSpawner;

    [Header("Time Settings")]
    public float dayDurationMinutes = 1f;
    public float nightDurationMinutes = 1f;

    [Header("Lighting")]
    public Light sunLight;
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int enemiesPerNight = 5;

    [Header("State (Read Only)")]
    public bool isNight = false;
    public float currentPhaseTimer = 0f;

    private List<GameObject> activeEnemies = new List<GameObject>();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartDay();
    }

    void Update()
    {
        // Чит на деньги для тестов
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"$$$ БАЛАНС: {money} | Ловушек: {trapsCount} | Камер: {camerasCount}");
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

    // --- МАГАЗИН И ИНВЕНТАРЬ ПРЕДМЕТОВ ---

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

    // Методы для использования предметов при строительстве
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

    // --- ИНВЕНТАРЬ МЕМОВ ---
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

    // --- ВРЕМЯ ---
    public void StartDay()
    {
        isNight = false;
        currentPhaseTimer = 0f;
        RenderSettings.fogColor = dayFog;
        RenderSettings.ambientIntensity = 1f;
        ClearEnemies();
        if (visitorSpawner != null) visitorSpawner.StartNewDay();
        Debug.Log(">>> ДЕНЬ");
    }

    public void StartNight()
    {
        isNight = true;
        currentPhaseTimer = 0f;
        RenderSettings.fogColor = nightFog;
        RenderSettings.ambientIntensity = 0.2f;
        if (visitorSpawner != null) visitorSpawner.StopSpawning();
        SpawnEnemies();
        Debug.Log(">>> НОЧЬ");
    }

    public void SkipCurrentPhase()
    {
        if (isNight) StartDay();
        else StartNight();
    }

    void SpawnEnemies()
    {
        if (spawnPoints.Length == 0) return;
        ClearEnemies();
        for (int i = 0; i < enemiesPerNight; i++)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject newEnemy = Instantiate(enemyPrefab, randomPoint.position, Quaternion.identity);
            var ai = newEnemy.GetComponent<EnemyAi>();
            if (ai != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player) ai.SetTarget(player.transform);
                ai.StartPatrolWithDetection();
            }
            activeEnemies.Add(newEnemy);
        }
    }

    void ClearEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();
    }
}