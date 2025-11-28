using UnityEngine;
using System.Collections.Generic;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Time Settings")]
    public float dayDurationMinutes = 10f;   // Длительность дня в минутах
    public float nightDurationMinutes = 10f; // Длительность ночи в минутах

    [Header("Lighting")]
    public Light sunLight; // Ссылка на Directional Light
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    [Header("Enemies")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints;
    public int enemiesPerNight = 5;

    [Header("State (Read Only)")]
    public bool isNight = false;
    public float currentPhaseTimer = 0f; // Текущее время в секундах

    private List<GameObject> activeEnemies = new List<GameObject>();

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        // При старте игры начинаем день
        StartDay();
    }

    void Update()
    {
        // 1. Увеличиваем таймер
        currentPhaseTimer += Time.deltaTime;

        // 2. Логика для ДНЯ
        if (!isNight)
        {
            float dayDurationSec = dayDurationMinutes * 60f;

            // Вращаем солнце (от -90 до 90 градусов, имитация дня)
            if (sunLight)
            {
                float progress = currentPhaseTimer / dayDurationSec;
                // Поворот солнца вокруг оси X: 0 (утро) -> 180 (вечер)
                float angle = Mathf.Lerp(0f, 180f, progress);
                sunLight.transform.rotation = Quaternion.Euler(angle, 0, 0);
                sunLight.intensity = 1f; // Днем ярко
            }

            // Если время вышло -> Включаем НОЧЬ
            if (currentPhaseTimer >= dayDurationSec)
            {
                StartNight();
            }
        }
        // 3. Логика для НОЧИ
        else
        {
            float nightDurationSec = nightDurationMinutes * 60f;

            // Вращаем луну/солнце дальше (от 180 до 360)
            if (sunLight)
            {
                float progress = currentPhaseTimer / nightDurationSec;
                float angle = Mathf.Lerp(180f, 360f, progress);
                sunLight.transform.rotation = Quaternion.Euler(angle, 0, 0);
                sunLight.intensity = 0.1f; // Ночью темно
            }

            // Если время вышло -> Включаем ДЕНЬ
            if (currentPhaseTimer >= nightDurationSec)
            {
                StartDay();
            }
        }
    }

    public void StartDay()
    {
        isNight = false;
        currentPhaseTimer = 0f;

        // Настройка атмосферы
        RenderSettings.fogColor = dayFog;
        RenderSettings.ambientIntensity = 1f;

        // Удаляем врагов (они сгорают на солнце или прячутся)
        ClearEnemies();

        Debug.Log(">>> Наступил ДЕНЬ (Таймер сброшен)");
    }

    public void StartNight()
    {
        isNight = true;
        currentPhaseTimer = 0f;

        // Настройка атмосферы
        RenderSettings.fogColor = nightFog;
        RenderSettings.ambientIntensity = 0.2f;

        // Спавним врагов
        SpawnEnemies();

        Debug.Log(">>> Наступила НОЧЬ (Таймер сброшен)");
    }

    // Метод для кровати (Пропуск текущей фазы)
    public void SkipCurrentPhase()
    {
        if (isNight)
        {
            StartDay(); // Если была ночь -> Сразу утро
        }
        else
        {
            StartNight(); // Если был день -> Сразу ночь
        }
    }

    void SpawnEnemies()
    {
        if (spawnPoints.Length == 0) return;

        // Очищаем старых на всякий случай
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
