using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("Tycoon Economy")]
    public float money = 100f;        // ������
    public int capturedCreatures = 0; // ��������� �����
    public float pricePerMeme = 1.5f;

    [Header("Items Inventory")]
    public int trapsCount = 2;   // ���������� �������
    public int camerasCount = 1; // ���������� �����
    public float trapPrice = 20f;
    public float cameraPrice = 15f;

    // ������ �������� �������� � �����
    public List<ParkPlatform> activePlatforms = new List<ParkPlatform>();

    [Header("Spawners")]
    public VisitorSpawner visitorSpawner; // ������ �� ������� �����
    public EnemySpawner enemySpawner;     // ������ �� ������� ������ (�����)

    [Header("Time Settings")]
    public float dayDurationMinutes = 1f;
    public float nightDurationMinutes = 1f;

    [Header("Lighting")]
    public Light sunLight;
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    [Header("State (Read Only)")]
    public bool isNight = false;
    public float currentPhaseTimer = 0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        StartDay();
        if (enemySpawner == null)
        {
            Debug.Log("��� enimySpavner.cs � GameManager");
        }
    }

    void Update()
    {
        // ��� �� �������� �������
        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log($"$$$ ������: {money} | �������: {trapsCount} | �����: {camerasCount}");
        }

        currentPhaseTimer += Time.deltaTime;

        if (!isNight) // ����
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
        else // ����
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

    // --- ������� � �������� ---

    public bool BuyTrap()
    {
        if (money >= trapPrice)
        {
            money -= trapPrice;
            trapsCount++;
            Debug.Log("������� �������!");
            return true;
        }
        Debug.Log("�� ������� ����� �� �������!");
        return false;
    }

    public bool BuyCamera()
    {
        if (money >= cameraPrice)
        {
            money -= cameraPrice;
            camerasCount++;
            Debug.Log("������� ������!");
            return true;
        }
        Debug.Log("�� ������� ����� �� ������!");
        return false;
    }

    // ������ ��� ������������� ��� �������������
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

    // --- ��������� ����� ---
    public void AddCreature()
    {
        capturedCreatures++;
        Debug.Log($"[���������] ��� ������! � �����: {capturedCreatures}");
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
        Debug.Log($"+++ �������: +{amount}. �����: {money}");
    }

    // --- ����� ��� ---
    public void StartDay()
    {
        isNight = false;
        currentPhaseTimer = 0f;
        RenderSettings.fogColor = dayFog;
        RenderSettings.ambientIntensity = 1f;

        // ���������� � ������� ��������
        if (enemySpawner != null) enemySpawner.ClearEnemies();
        if (visitorSpawner != null) visitorSpawner.StartNewDay();

        Debug.Log(">>> ���� (���� ������)");
    }

    public void StartNight()
    {
        isNight = true;
        currentPhaseTimer = 0f;
        RenderSettings.fogColor = nightFog;
        RenderSettings.ambientIntensity = 0.2f;

        // ���������� � ������� ��������
        if (visitorSpawner != null) visitorSpawner.StopSpawning();
        if (enemySpawner != null) enemySpawner.SpawnEnemies();

        Debug.Log(">>> ���� (����� ��������)");
    }

    public void SkipCurrentPhase()
    {
        if (isNight) StartDay();
        else StartNight();
    }
}