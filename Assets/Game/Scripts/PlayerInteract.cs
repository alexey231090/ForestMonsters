using UnityEngine;

// Этот атрибут гарантирует, что скрипт переноски тоже висит на игроке
[RequireComponent(typeof(PlayerCarrier))]
public class PlayerInteract : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2, 5)] public string description = "Интеракция: Установка (ЛКМ), Призраки с автоотключением, Взаимодействие (E).";

    [Header("Settings")]
    public float interactDistance = 4f;     // Дистанция для E (кнопок)
    public float buildDistance = 10f;       // Дистанция для СТРОИТЕЛЬСТВА
    public float ghostTimeout = 5.0f;       // Время до исчезновения призрака (если не смотреть на землю)
    
    public LayerMask interactLayer; // Слой предметов (ловушки, мониторы)
    public LayerMask groundLayer;   // Слой для СТРОИТЕЛЬСТВА (земля/пол)

    [Header("Prefabs (Real)")]
    public GameObject trapPrefab;
    public GameObject cameraItemPrefab;

    [Header("Prefabs (Ghosts)")]
    public GameObject trapGhostPrefab;
    public GameObject cameraGhostPrefab;

    [Header("References")]
    public Transform cameraPrefab;
    public CctvManager cctvManager;
    
    private PlayerCarrier carrier;

    [Header("Placement Offsets")]
    public float trapEmbedDepth = 0f;
    public float cameraEmbedDepth = 0f;
    public float trapGhostOffset = 0f;
    public float cameraGhostOffset = 0f;

    // --- ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ---
    private int selectedItemIndex = -1; // -1 значит "Ничего не выбрано"
    private GameObject currentGhost;
    private float ghostTimer = 0f; // Текущий таймер жизни призрака

    void Start()
    {
        carrier = GetComponent<PlayerCarrier>();
    }

    void Update()
    {
        Transform origin = cameraPrefab;
        if (origin == null)
        {
            if (Camera.main != null) origin = Camera.main.transform;
            else origin = transform;
        }
        
        Debug.DrawRay(origin.position, origin.forward * interactDistance, Color.red);

        // 1. ЕСЛИ МЫ НЕСЕМ КЛЕТКУ
        if (carrier.IsCarrying())
        {
            DisableBuildMode(); // Выключаем режим стройки при переноске
            return; 
        }

        // 2. ВЫБОР ПРЕДМЕТА
        if (Input.GetKeyDown(KeyCode.Alpha1)) 
        {
            Debug.Log($"[INPUT] Нажата 1. Ловушек: {GameManager.instance.trapsCount}");
            ChangeItem(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) 
        {
            Debug.Log($"[INPUT] Нажата 2. Камер: {GameManager.instance.camerasCount}");
            ChangeItem(1);
        }

        // 3. ПРИЗРАК И ТАЙМЕР
        UpdateGhostLogic(origin);

        // 4. УСТАНОВКА ПРЕДМЕТА (ЛКМ)
        // Разрешаем только если режим стройки активен (не -1) и призрак виден
        if (selectedItemIndex != -1 && Input.GetMouseButtonDown(0))
        {
            TryPlaceItem(origin);
        }

        // 5. ВЗАИМОДЕЙСТВИЕ (E)
        HandleInteraction(origin);
    }

    // ================== ЛОГИКА ПРИЗРАКОВ И ТАЙМЕРА ==================

    void UpdateGhostLogic(Transform origin)
    {
        // Если ничего не выбрано - выходим
        if (selectedItemIndex == -1) 
        {
            DestroyGhost();
            return;
        }

        // 1. Проверяем наличие предметов
        bool hasItem = false;
        if (selectedItemIndex == 0 && GameManager.instance.trapsCount > 0) hasItem = true;
        if (selectedItemIndex == 1 && GameManager.instance.camerasCount > 0) hasItem = true;

        if (!hasItem) 
        { 
            DisableBuildMode(); // Кончились предметы - выключаем режим
            return; 
        }

        RaycastHit hit;
        
        // 2. Ищем землю
        if (Physics.Raycast(origin.position, origin.forward, out hit, buildDistance, groundLayer))
        {
            // --- МЫ СМОТРИМ НА ЗЕМЛЮ ---
            ghostTimer = ghostTimeout; // Сбрасываем таймер на максимум (5 сек)

            // Логика создания/перемещения призрака
            GameObject neededGhostPrefab = (selectedItemIndex == 0) ? trapGhostPrefab : cameraGhostPrefab;

            if (currentGhost == null) currentGhost = Instantiate(neededGhostPrefab);
            else if (!currentGhost.name.Contains(neededGhostPrefab.name))
            {
                DestroyGhost();
                currentGhost = Instantiate(neededGhostPrefab);
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            float ghostHeightAdjust = (selectedItemIndex == 0) ? trapGhostOffset : cameraGhostOffset;

            if (selectedItemIndex == 1) // Поворот камеры
            {
                Vector3 lookPos = transform.position - hit.point;
                lookPos.y = 0;
                if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
            }

            Vector3 position = hit.point + (hit.normal * ghostHeightAdjust);
            currentGhost.transform.position = position;
            currentGhost.transform.rotation = rotation;
        }
        else
        {
            // --- МЫ СМОТРИМ В НЕБО ---
            DestroyGhost(); // Прячем визуал

            // Уменьшаем таймер
            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0)
            {
                DisableBuildMode(); // Время вышло - отключаем режим
                Debug.Log("Режим строительства отключен из-за бездействия.");
            }
        }
    }

    void ChangeItem(int index)
    {
        selectedItemIndex = index;
        ghostTimer = ghostTimeout; // При смене предмета таймер обновляется
        DestroyGhost();
    }

    void DisableBuildMode()
    {
        selectedItemIndex = -1;
        DestroyGhost();
    }

    void DestroyGhost()
    {
        if (currentGhost != null)
        {
            Destroy(currentGhost);
            currentGhost = null;
        }
    }

    // ================== ЛОГИКА УСТАНОВКИ ==================

    void TryPlaceItem(Transform origin)
    {
        // Если призрака нет (значит мы смотрим не туда), ставить нельзя
        if (currentGhost == null) return;

        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, buildDistance, groundLayer))
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
                    currentRealDepth = trapEmbedDepth;
                }
            }
            else if (selectedItemIndex == 1)
            {
                if (GameManager.instance.TryUseCamera())
                {
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentRealDepth = cameraEmbedDepth;
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

                Vector3 position = hit.point - hit.normal * currentRealDepth;
                Instantiate(objectToSpawn, position, rotation);
                
                // После установки таймер обновляем, чтобы можно было ставить дальше
                ghostTimer = ghostTimeout; 
            }
        }
    }

    // ================== ЛОГИКА ВЗАИМОДЕЙСТВИЯ (Без изменений) ==================

    void HandleInteraction(Transform origin)
    {
        RaycastHit hit;
        bool lookingAtPickupable = false;

        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            bool isTrap = hit.collider.GetComponentInChildren<Trap>(true) != null;
            bool isCamera = hit.collider.GetComponentInChildren<Camera>(true) != null;

            if (isTrap || isCamera)
            {
                lookingAtPickupable = true;
                if (Input.GetKey(KeyCode.E)) carrier.ProcessHold(hit.collider.gameObject);
                return; 
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
                if (monitor != null && CctvManager.instance != null && !CctvManager.instance.isMonitorActive)
                {
                    print("monitor");
                    CctvManager.instance.EnterMonitorMode(); return;
                }

                BedTrigger bed = hit.collider.GetComponent<BedTrigger>();
                if (bed != null && GameManager.instance != null) { GameManager.instance.SkipCurrentPhase(); return; }

                ParkPlatform platform = hit.collider.GetComponent<ParkPlatform>();
                if (platform != null) { platform.TryPlaceMonster(); return; }
            }
        }

        if (!lookingAtPickupable) carrier.ResetHoldTimer();
    }
}