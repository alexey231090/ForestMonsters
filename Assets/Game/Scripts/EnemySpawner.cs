using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : SignalBinder
{
    [Header("Subscribed Events")]
    [SerializeField] private GameEvent GET_onDayStarted;
    [SerializeField] private GameEvent GET_onNightStarted;

    [Header("Enemies Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints; // Spawn points for enemies
    public int enemiesPerNight = 3; // Maximum number of enemies to spawn
    public float spawnRadius = 2.0f; // Радиус случайного смещения от точки спавна

    private void Start()
    {
        Bind(GET_onDayStarted, ClearEnemies);
        Bind(GET_onNightStarted, SpawnEnemies);
    }

    // Класс для связи врага с его точкой спавна
    private class ActiveEnemyInfo
    {
        public GameObject enemy;
        public Transform originalPoint;
    }

    private List<ActiveEnemyInfo> activeEnemiesInfo = new List<ActiveEnemyInfo>();
    private List<Transform> fixedSpawnPoints = new List<Transform>();

    public void SpawnEnemies()
    {
        Debug.Log("[ENEMY SPAWNER] Received Night Started signal!");
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return;
        }

        ClearEnemies();

        // 1. Если точек меньше чем нужно (кто-то был пойман), добираем новые рандомные точки
        if (fixedSpawnPoints.Count < enemiesPerNight)
        {
            int pointsNeeded = enemiesPerNight - fixedSpawnPoints.Count;
            
            // Создаем список только из тех точек, которые СЕЙЧАС не используются
            List<Transform> availablePool = new List<Transform>(spawnPoints);
            foreach (var p in fixedSpawnPoints) availablePool.Remove(p);

            int actualToTake = Mathf.Min(pointsNeeded, availablePool.Count);
            for (int i = 0; i < actualToTake; i++)
            {
                int randomIndex = Random.Range(0, availablePool.Count);
                fixedSpawnPoints.Add(availablePool[randomIndex]);
                availablePool.RemoveAt(randomIndex);
            }
            
            Debug.Log($"[SPAWNER] Re-randomized {actualToTake} points. Total fixed points: {fixedSpawnPoints.Count}");
        }

        // 2. Спавним врагов на всех закрепленных точках
        for (int i = 0; i < fixedSpawnPoints.Count; i++)
        {
            Transform selectedPoint = fixedSpawnPoints[i];

            // Вычисляем случайное положение на окружности
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * spawnRadius;
            Vector3 spawnPos = selectedPoint.position + offset;

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

            // Настройка AI
            var ai = newEnemy.GetComponent<EnemyAi>();
            if (ai != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player) ai.SetTarget(player.transform);
                ai.StartPatrolWithDetection();
            }

            // Запоминаем инфо о враге и его точке
            activeEnemiesInfo.Add(new ActiveEnemyInfo { enemy = newEnemy, originalPoint = selectedPoint });
        }

        Debug.Log($"Total enemies spawned: {activeEnemiesInfo.Count} enemies.");
    }

    public void ClearEnemies()
    {
        Debug.Log($"[SPAWNER] Clearing {activeEnemiesInfo.Count} enemies.");
        List<ActiveEnemyInfo> nextNightEnemies = new List<ActiveEnemyInfo>();

        foreach (var info in activeEnemiesInfo)
        {
            if (info.enemy == null) continue;

            var ai = info.enemy.GetComponent<EnemyAi>();
            
            // Если враг НЕ пойман
            if (ai != null && !ai.IsCaught)
            {
                // Удаляем его из мира (утром)
                Destroy(info.enemy);
                // Точка остается в fixedSpawnPoints (мы ничего не удаляем из него здесь)
            }
            else
            {
                // Враг пойман! Оставляем его в мире (он в ловушке)
                // Но его точка должна быть ПЕРЕРАСПРЕДЕЛЕНА на следующую ночь.
                fixedSpawnPoints.Remove(info.originalPoint);
                Debug.Log($"[SPAWNER] Enemy caught! Point '{info.originalPoint.name}' released for re-randomization.");
            }
        }

        // Очищаем список заспавненных для новой итерации
        activeEnemiesInfo.Clear();
    }
}