using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemies Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints; // Все возможные точки на карте
    public int enemiesPerNight = 3; // Сколько хотим заспавнить

    private List<GameObject> activeEnemies = new List<GameObject>();

    public void SpawnEnemies()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("Нет точек спавна!");
            return;
        }

        ClearEnemies();

        // 1. Создаем временный список доступных точек
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        // 2. Решаем, сколько врагов спавнить (не больше, чем есть точек)
        int countToSpawn = Mathf.Min(enemiesPerNight, availablePoints.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            // 3. Выбираем случайную точку
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            // 4. Удаляем точку из списка, чтобы не повторилась
            availablePoints.RemoveAt(randomIndex);

            // 5. Спавним врага
            GameObject newEnemy = Instantiate(enemyPrefab, selectedPoint.position, Quaternion.identity);

            // --- НОВОЕ: Выводим в консоль имя точки ---
            Debug.Log($"[SPAWN] Враг #{i + 1} появился на точке: {selectedPoint.name}");
            // ------------------------------------------

            // Настраиваем AI
            var ai = newEnemy.GetComponent<EnemyAi>();
            if (ai != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player) ai.SetTarget(player.transform);
                ai.StartPatrolWithDetection();
            }

            activeEnemies.Add(newEnemy);
        }

        Debug.Log($"Всего заспавнено: {activeEnemies.Count} врагов.");
    }

    public void ClearEnemies()
    {
        foreach (var enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();
    }
}