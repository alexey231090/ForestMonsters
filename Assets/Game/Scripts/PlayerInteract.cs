using UnityEngine;

// Этот атрибут гарантирует, что скрипт переноски тоже висит на игроке
[RequireComponent(typeof(PlayerCarrier))]
public class PlayerInteract : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2, 5)] public string description = "Интеракция: Установка (ЛКМ), Призраки, Взаимодействие (E). Переноску (Hold E) делегирует в PlayerCarrier.";

    [Header("Settings")]
    public float interactDistance = 4f;     // Дистанция для E (кнопок)
    public float buildDistance = 10f;       // Дистанция для СТРОИТЕЛЬСТВА
    
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
    
    // Ссылка на соседний скрипт, который занимается переноской
    private PlayerCarrier carrier;

    [Header("Placement Offsets")]
    public float trapEmbedDepth = 0f;
    public float cameraEmbedDepth = 0f;
    public float trapGhostOffset = 0f;
    public float cameraGhostOffset = 0f;

    // --- ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ---
    private int selectedItemIndex = 0; 
    private GameObject currentGhost;

    void Start()
    {
        // Находим соседний скрипт
        carrier = GetComponent<PlayerCarrier>();
    }

    void Update()
    {
        // Определяем точку, откуда смотрим
        Transform origin = cameraPrefab;
        if (origin == null)
        {
            if (Camera.main != null) origin = Camera.main.transform;
            else origin = transform;
        }
        
        Debug.DrawRay(origin.position, origin.forward * interactDistance, Color.red);

        // 1. ЕСЛИ МЫ НЕСЕМ КЛЕТКУ (через Carrier)
        // Мы выключаем призраков и возможность строить, чтобы не мешать
        if (carrier.IsCarrying())
        {
            DestroyGhost();
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

        // 3. ПРИЗРАК (Визуализация строительства)
        UpdateGhost(origin);

        // 4. УСТАНОВКА ПРЕДМЕТА (ЛКМ)
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceItem(origin);
        }

        // 5. ВЗАИМОДЕЙСТВИЕ (E)
        HandleInteraction(origin);
    }

    // ================== ЛОГИКА ВЗАИМОДЕЙСТВИЯ ==================

    void HandleInteraction(Transform origin)
    {
        RaycastHit hit;
        bool lookingAtPickupable = false;

        // Пускаем луч
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            // --- A. ПРОВЕРКА НА ПОДБОР (Передаем в Carrier) ---
            
            // Проверяем, это ловушка?
            bool isTrap = hit.collider.GetComponentInParent<Trap>() != null || hit.collider.GetComponent<Trap>() != null;
            // Проверяем, это камера?
            bool isCamera = hit.collider.GetComponentInChildren<Camera>() != null || hit.collider.name.Contains("Camera");

            if (isTrap || isCamera)
            {
                lookingAtPickupable = true;
                
                // Если держим E - говорим Carrier'у "Заряжай круг!"
                if (Input.GetKey(KeyCode.E))
                {
                    // Передаем объект, на который смотрим
                    carrier.ProcessHold(hit.collider.gameObject);
                }
                return; // Блокируем остальные действия
            }

            // --- B. МОМЕНТАЛЬНЫЕ ДЕЙСТВИЯ (КЛИК E) ---
            if (Input.GetKeyDown(KeyCode.E))
            {
                // 1. Монитор
                MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
                if (monitor != null && CctvManager.instance != null)
                {
                    CctvManager.instance.EnterMonitorMode();
                    return;
                }

                // 2. Кровать
                BedTrigger bed = hit.collider.GetComponent<BedTrigger>();
                if (bed != null && GameManager.instance != null)
                {
                    GameManager.instance.SkipCurrentPhase();
                    return;
                }

                // 3. Платформа в парке
                ParkPlatform platform = hit.collider.GetComponent<ParkPlatform>();
                if (platform != null)
                {
                    platform.TryPlaceMonster();
                    return;
                }
            }
        }

        // Если мы отвели взгляд от предмета или отпустили кнопку
        if (!lookingAtPickupable)
        {
            carrier.ResetHoldTimer();
        }
    }

    // ================== ЛОГИКА ПРИЗРАКОВ ==================

    void UpdateGhost(Transform origin)
    {
        // 1. Проверяем наличие предметов в инвентаре
        bool hasItem = false;
        if (selectedItemIndex == 0 && GameManager.instance.trapsCount > 0) hasItem = true;
        if (selectedItemIndex == 1 && GameManager.instance.camerasCount > 0) hasItem = true;

        if (!hasItem) { DestroyGhost(); return; }

        RaycastHit hit;
        
        // 2. Ищем землю для призрака
        if (Physics.Raycast(origin.position, origin.forward, out hit, buildDistance, groundLayer))
        {
            GameObject neededGhostPrefab = (selectedItemIndex == 0) ? trapGhostPrefab : cameraGhostPrefab;

            // Создаем или заменяем призрака
            if (currentGhost == null) currentGhost = Instantiate(neededGhostPrefab);
            else if (!currentGhost.name.Contains(neededGhostPrefab.name))
            {
                DestroyGhost();
                currentGhost = Instantiate(neededGhostPrefab);
            }

            // Позиционирование
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            float ghostHeightAdjust = (selectedItemIndex == 0) ? trapGhostOffset : cameraGhostOffset;

            if (selectedItemIndex == 1) // Поворот камеры к игроку
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
            DestroyGhost();
        }
    }

    // ================== ЛОГИКА УСТАНОВКИ ==================

    void TryPlaceItem(Transform origin)
    {
        RaycastHit hit;
        // Ищем землю для установки
        if (Physics.Raycast(origin.position, origin.forward, out hit, buildDistance, groundLayer))
        {
            bool canPlace = false;
            GameObject objectToSpawn = null;
            float currentRealDepth = 0f;

            // Логика списания ресурсов
            if (selectedItemIndex == 0) // Ловушка
            {
                if (GameManager.instance.TryUseTrap())
                {
                    canPlace = true;
                    objectToSpawn = trapPrefab;
                    currentRealDepth = trapEmbedDepth;
                }
                else Debug.Log("Нет ловушек!");
            }
            else if (selectedItemIndex == 1) // Камера
            {
                if (GameManager.instance.TryUseCamera())
                {
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentRealDepth = cameraEmbedDepth;
                }
                else Debug.Log("Нет камер!");
            }

            // Спавн объекта
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
            }
        }
    }

    // ================== ВСПОМОГАТЕЛЬНЫЕ ==================

    void ChangeItem(int index)
    {
        selectedItemIndex = index;
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
}