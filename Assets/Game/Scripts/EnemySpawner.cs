using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : SignalBinder
{
    [Header("Subscribed Events")]
    [SerializeField] private GameEvent GET_onDayStarted;
    [SerializeField] private GameEvent GET_onNightStarted;

    [Header("Enemies Settings")]
    public GameObject[] enemyPrefabs; // Массив видов врагов
    public Transform[] spawnPoints;   // Точки спавна
    public int enemiesPerNight = 3;   // Максимальное количество врагов
    public float spawnRadius = 2.0f;  // Радиус случайного смещения

    [System.Serializable]
    public class SpawnAssignment
    {
        public Transform point;
        public GameObject prefab;
    }

    private void Start()
    {
        Bind(GET_onDayStarted, ClearEnemies);
        Bind(GET_onNightStarted, SpawnEnemies);
    }

    // Класс для связи текущего объекта врага в мире с его назначением
    private class ActiveEnemyInfo
    {
        public GameObject enemy;
        public SpawnAssignment assignment;
    }

    private List<ActiveEnemyInfo> activeEnemiesInfo = new List<ActiveEnemyInfo>();
    [SerializeField] private List<SpawnAssignment> activeAssignments = new List<SpawnAssignment>();

    public void SpawnEnemies()
    {
        Debug.Log("[ENEMY SPAWNER] Received Night Started signal!");
        if (spawnPoints == null || spawnPoints.Length == 0 || enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogWarning("No spawn points or enemy prefabs available!");
            return;
        }

        ClearEnemies();

        // 1. Если назначений меньше чем нужно (кто-то был пойман или старт игры), создаем новые
        if (activeAssignments.Count < enemiesPerNight)
        {
            int needed = enemiesPerNight - activeAssignments.Count;
            
            // Собираем список свободных точек
            List<Transform> availablePoints = new List<Transform>(spawnPoints);
            foreach (var a in activeAssignments) availablePoints.Remove(a.point);

            int toCreate = Mathf.Min(needed, availablePoints.Count);
            for (int i = 0; i < toCreate; i++)
            {
                int pointIdx = Random.Range(0, availablePoints.Count);
                int prefabIdx = Random.Range(0, enemyPrefabs.Length);

                activeAssignments.Add(new SpawnAssignment 
                { 
                    point = availablePoints[pointIdx], 
                    prefab = enemyPrefabs[prefabIdx] 
                });

                availablePoints.RemoveAt(pointIdx);
            }
            Debug.Log($"[SPAWNER] Created {toCreate} new assignments.");
        }

        // 2. Спавним врагов согласно их постоянным назначениям
        foreach (var assignment in activeAssignments)
        {
            if (assignment.point == null || assignment.prefab == null) continue;

            // Смещение
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            Vector3 offset = new Vector3(randomDir.x, 0, randomDir.y) * spawnRadius;
            Vector3 spawnPos = assignment.point.position + offset;

            GameObject newEnemy = Instantiate(assignment.prefab, spawnPos, Quaternion.identity);

            // Настройка AI
            var ai = newEnemy.GetComponent<EnemyAi>();
            if (ai != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player) ai.SetTarget(player.transform);
                ai.StartPatrolWithDetection();
            }

            // Запоминаем связь живого объекта с его назначением
            activeEnemiesInfo.Add(new ActiveEnemyInfo { enemy = newEnemy, assignment = assignment });
        }

        Debug.Log($"[SPAWNER] Total enemies spawned: {activeEnemiesInfo.Count}");
    }

    public void ClearEnemies()
    {
        Debug.Log($"[SPAWNER] Clearing {activeEnemiesInfo.Count} enemies.");

        foreach (var info in activeEnemiesInfo)
        {
            if (info.enemy == null) continue;

            var ai = info.enemy.GetComponent<EnemyAi>();
            
            // Если враг пойман
            if (ai != null && ai.IsCaught)
            {
                // Враг пойман! Удаляем назначение, чтобы в след. раз был рандом
                activeAssignments.Remove(info.assignment);
                Debug.Log($"[SPAWNER] Enemy at {info.assignment.point.name} was caught! Assignment removed.");
            }
            else
            {
                // Враг НЕ пойман — просто удаляем объект (утром), но назначение остается
                Destroy(info.enemy);
            }
        }

        // Очищаем текущий список живых врагов
        activeEnemiesInfo.Clear();
    }
}
