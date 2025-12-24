using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2, 5)] public string description = "Интеракция игрока. Тратит предметы из инвентаря GameManager при установке.";

    [Header("Settings")]
    public float interactDistance = 4f;
    public LayerMask interactLayer;

    [Header("Prefabs (Real)")]
    public GameObject trapPrefab;
    public GameObject cameraItemPrefab;

    [Header("Prefabs (Ghosts/Preview)")]
    public GameObject trapGhostPrefab;
    public GameObject cameraGhostPrefab;

    [Header("References")]
    public Transform cameraPrefab;
    public CctvManager cctvManager;

    [Header("Real Object Settings (Offsets)")]
    public float trapEmbedDepth = 0f;   // Насколько глубоко ставится РЕАЛЬНАЯ ловушка
    public float cameraEmbedDepth = 0f; // Насколько глубоко ставится РЕАЛЬНАЯ камера

    [Header("Ghost Visual Settings (Offsets)")]
    public float trapGhostOffset = 0f;   // Корректировка высоты ТОЛЬКО для призрака ловушки
    public float cameraGhostOffset = 0f; // Корректировка высоты ТОЛЬКО для призрака камеры

    private int selectedItemIndex = 0; // 0 = Trap, 1 = Camera
    private GameObject currentGhost;   

    void Update()
    {
        var origin = cameraPrefab != null ? cameraPrefab : (Camera.main != null ? Camera.main.transform : transform);
        
        // Рисуем дебаг луч
        Debug.DrawRay(origin.position, origin.forward * interactDistance, Color.red);

        // --- ВЫБОР ПРЕДМЕТА ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ChangeItem(0);
            Debug.Log($"Выбрана: ЛОВУШКА (У вас: {GameManager.instance.trapsCount})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ChangeItem(1);
            Debug.Log($"Выбрана: КАМЕРА (У вас: {GameManager.instance.camerasCount})");
        }

        // --- ОБНОВЛЕНИЕ ПРИЗРАКА ---
        UpdateGhost(origin);

        // --- УСТАНОВКА (ЛКМ) ---
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceItem(origin);
        }

        // --- ВЗАИМОДЕЙСТВИЕ (E) ---
        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteract(origin);
        }
    }

    void ChangeItem(int index)
    {
        selectedItemIndex = index;
        DestroyGhost(); 
    }

    void UpdateGhost(Transform origin)
    {
        // 1. Проверяем наличие предметов
        bool hasItem = false;
        if (selectedItemIndex == 0 && GameManager.instance.trapsCount > 0) hasItem = true;
        if (selectedItemIndex == 1 && GameManager.instance.camerasCount > 0) hasItem = true;

        if (!hasItem)
        {
            DestroyGhost();
            return;
        }

        // 2. Пускаем луч
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            GameObject neededGhostPrefab = (selectedItemIndex == 0) ? trapGhostPrefab : cameraGhostPrefab;
            
            if (currentGhost == null)
            {
                currentGhost = Instantiate(neededGhostPrefab);
            }
            else if (currentGhost.name.Contains(neededGhostPrefab.name) == false) 
            {
                DestroyGhost();
                currentGhost = Instantiate(neededGhostPrefab);
            }

            // 3. Расчет позиции
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            // Выбираем СПЕЦИАЛЬНЫЙ оффсет для призрака
            float ghostHeightAdjust = (selectedItemIndex == 0) ? trapGhostOffset : cameraGhostOffset;

            if (selectedItemIndex == 1) // Поворот для камеры
            {
                Vector3 lookPos = transform.position - hit.point;
                lookPos.y = 0;
                if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
            }

            // Применяем позицию с учетом оффсета призрака
            // Мы прибавляем normal * offset (двигаем вверх/вниз по нормали)
            Vector3 position = hit.point + (hit.normal * ghostHeightAdjust);

            currentGhost.transform.position = position;
            currentGhost.transform.rotation = rotation;
        }
        else
        {
            DestroyGhost();
        }
    }

    void DestroyGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
    }

    void TryPlaceItem(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            bool canPlace = false;
            GameObject objectToSpawn = null;
            float currentRealDepth = 0f;

            if (selectedItemIndex == 0) 
            {
                if (GameManager.instance.TryUseTrap())
                {
                    canPlace = true;
                    objectToSpawn = trapPrefab;
                    currentRealDepth = trapEmbedDepth; // Используем настройку для РЕАЛЬНОГО объекта
                }
                else
                {
                    Debug.Log("Нет ловушек! Купите в магазине.");
                }
            }
            else if (selectedItemIndex == 1) 
            {
                if (GameManager.instance.TryUseCamera())
                {
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentRealDepth = cameraEmbedDepth; // Используем настройку для РЕАЛЬНОГО объекта
                }
                else
                {
                    Debug.Log("Нет камер! Купите в магазине.");
                }
            }

            if (canPlace && objectToSpawn != null)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (selectedItemIndex == 1)
                {
                    Vector3 lookPos = transform.position - hit.point;
                    lookPos.y = 0;
                    if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
                }

                // Тут используем старую логику "вычитания" для погружения
                Vector3 position = hit.point - hit.normal * currentRealDepth;
                
                Instantiate(objectToSpawn, position, rotation);
            }
        }
    }

    void TryInteract(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance))
        {
            TrapBox trapbox = hit.collider.GetComponentInParent<TrapBox>(); 
            Trap trap = hit.collider.GetComponent<Trap>();

            if (trapbox != null)
            {
                trap = trapbox.GetComponentInChildren<Trap>();
            }

            if (trap != null)
            {
                if (trap.HasCatch()) trap.CollectPrey();
                return;
            }

            MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
            if (monitor != null)
            {
                if (CctvManager.instance != null) CctvManager.instance.EnterMonitorMode();
                return;
            }

            BedTrigger bed = hit.collider.GetComponent<BedTrigger>();
            if (bed != null)
            {
                if (GameManager.instance != null) GameManager.instance.SkipCurrentPhase();
                return;
            }

            ParkPlatform platform = hit.collider.GetComponent<ParkPlatform>();
            if (platform != null)
            {
                platform.TryPlaceMonster();
                return;
            }
        }
    }
}