using UnityEngine;

public class CameraSensor : MonoBehaviour
{
    [Header("Settings")]
    public GameObject markerPrefab; // Сюда положим префаб красной сферы
    public float detectionCooldown = 5f; // Задержка (в секундах), чтобы не спамить сферами

    private float nextSpawnTime = 0f;

    void OnTriggerEnter(Collider other)
    {
        // 1. Проверяем кулдаун (перезарядку)
        if (Time.time < nextSpawnTime) return;

        // 2. Проверяем, что зашел Враг
        if (other.CompareTag("Enemy"))
        {
            SpawnMarker(other.transform.position);
            print("Враг замечен");

            // Ставим задержку перед следующим срабатыванием
            nextSpawnTime = Time.time + detectionCooldown;
        }
    }

    void SpawnMarker(Vector3 enemyPos)
    {
        // Корректируем позицию, чтобы сфера была на земле (Y = 0 или чуть выше)
        Vector3 spawnPos = new Vector3(enemyPos.x, 0.1f, enemyPos.z);

        // Создаем сферу
        Instantiate(markerPrefab, spawnPos, Quaternion.identity);

        Debug.Log("Камера засекла движение! Метка создана.");
    }
}
