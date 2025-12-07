using UnityEngine;

public class CameraSensor : MonoBehaviour
{
    [Header("Settings")]
    public GameObject markerPrefab; // Marker prefab
    public float detectionCooldown = 5f; // Detection cooldown (in seconds), after which the marker can be spawned again
    public float spawnHeight = 0.1f; // Height at which the marker is spawned

    private float nextSpawnTime = 0f;

    void Start()
    {
        if(markerPrefab == null)
        {
            Debug.LogError("Marker prefab is not assigned!");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 1. Check cooldown (waiting time)
        if (Time.time < nextSpawnTime) return;

        // 2. Check if the object is an enemy
        if (other.CompareTag("Enemy"))
        {
            SpawnMarker(other.transform.position);
            print("Enemy detected");

            // Update the next spawn time
            nextSpawnTime = Time.time + detectionCooldown;
        }
    }

    void SpawnMarker(Vector3 enemyPos)
    {
        // Create marker spawn position using enemy position, but on the ground (Y = spawnHeight for ground)
        Vector3 spawnPos = new Vector3(enemyPos.x, spawnHeight, enemyPos.z);

        // Spawn the marker
        Instantiate(markerPrefab, spawnPos, Quaternion.identity);

        Debug.Log("Enemy marker created! Enemy detected.");
    }
}
