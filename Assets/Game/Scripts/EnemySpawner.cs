using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemies Settings")]
    public GameObject enemyPrefab;
    public Transform[] spawnPoints; // Spawn points for enemies
    public int enemiesPerNight = 3; // Maximum number of enemies to spawn

    private List<GameObject> activeEnemies = new List<GameObject>();

    public void SpawnEnemies()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return;
        }

        ClearEnemies();

        // 1. Create a list of available spawn points
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        // 2. Determine how many enemies to spawn (minimum of requested and available points)
        int countToSpawn = Mathf.Min(enemiesPerNight, availablePoints.Count);

        for (int i = 0; i < countToSpawn; i++)
        {
            // 3. Select a random spawn point
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            // 4. Remove the selected point from available points
            availablePoints.RemoveAt(randomIndex);

            // 5. Instantiate the enemy
            GameObject newEnemy = Instantiate(enemyPrefab, selectedPoint.position, Quaternion.identity);

            // --- Log: Spawn information ---
            Debug.Log($"[SPAWN] Enemy #{i + 1} spawned at point: {selectedPoint.name}");
            // ------------------------------------------

            // Setup AI
            var ai = newEnemy.GetComponent<EnemyAi>();
            if (ai != null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player) ai.SetTarget(player.transform);
                ai.StartPatrolWithDetection();
            }

            activeEnemies.Add(newEnemy);
        }

        Debug.Log($"Total enemies spawned: {activeEnemies.Count} enemies.");
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