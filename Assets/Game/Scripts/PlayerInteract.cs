using UnityEngine;

[RequireComponent(typeof(PlayerCarrier))]
public class PlayerInteract : SignalBinder
{
    [Header("Description")]
    [TextArea(2, 5)] public string description = "Интеракция: Установка (ЛКМ), Призраки с автоотключением, Взаимодействие (E).";

    [Header("Settings")]
    public float interactDistance = 8f;     // Дистанция для E (кнопок)
    public float buildDistance = 10f;       // Дистанция для СТРОИТЕЛЬСТВА ловушек
    public float cameraBuildDistance = 15f; // Дистанция для СТРОИТЕЛЬСТВА камер
    public bool cameraLookAtPlayer = true;  // Поворачивать ли камеру к игроку при установке
    public float ghostTimeout = 5.0f;       // Время до исчезновения призрака (если не смотреть на землю)
    
    public LayerMask interactLayer; // Слой предметов (ловушки, мониторы)
    public LayerMask groundLayer;   // Слой для СТРОИТЕЛЬСТВА (земля/пол)
    public LayerMask treeLayer;     // Слой для установки камер (деревья)

    [Header("Prefabs (Real)")]
    public GameObject trapPrefab;
    public GameObject cameraItemPrefab;

    [Header("Prefabs (Ghosts)")]
    public GameObject trapGhostPrefab;
    public GameObject cameraGhostPrefab;
    
    [Header("VFX")]
    public GameObject dustEffectPrefab;

    [Header("References")]
    public Transform cameraPrefab;
    public CctvManager cctvManager;
    
    private PlayerCarrier carrier;

    [Header("Placement Offsets")]
    public float trapEmbedDepth = 0f;
    public float cameraEmbedDepth = 0f;
    public float trapGhostOffset = 0f;
    public float cameraGhostOffset = 0f;

    [Header("VFX Offsets")]
    public float trapDustOffset = 0.1f;
    public float cameraDustOffset = 0.1f;

    [Header("Variables SO")]
    [SerializeField] IntVariable VAR_TrapsCount;
    [SerializeField] IntVariable VAR_CamerasCount;
    [SerializeField, Bind] IntVariable VAR_SelectedSlot;
    [SerializeField, Bind] FloatVariable VAR_BuildFuseProgress;
    [SerializeField, Bind] BoolVariable VAR_IsBuildFuseActive;
    [SerializeField] BoolVariable VAR_IsCarrying;
    [SerializeField, Bind] FloatVariable VAR_PickupProgress;

    [Header("Placement Hold Settings")]
    public float placeHoldTimeRequired = 0.5f; // Время удержания для установки
    [SerializeField] private float placeCooldownSeconds = 2.0f; // Задержка между установками

    // --- ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ---
    private GameObject currentGhost;
    private float ghostTimer = 0f; // Текущий таймер жизни призрака
    private bool wasLookingAtGround = false;
    private float placeHoldTimer = 0f; // Таймер удержания ЛКМ для установки
    private float placeCooldownTimer = 0f; // Таймер задержки между установками

    void OnEnable()
    {
        base.OnEnable();
        carrier = GetComponent<PlayerCarrier>();

        // Initial state sync
        if (VAR_SelectedSlot != null) VAR_SelectedSlot.Value = -1;
        if (VAR_IsBuildFuseActive != null) VAR_IsBuildFuseActive.Value = false;
    }

    // ================== AUTO-REACTION METHODS (via [Bind]) ==================

    private void OnVAR_SelectedSlotChanged()
    {
        ghostTimer = ghostTimeout;
        DestroyGhost();

        if (VAR_IsBuildFuseActive != null) VAR_IsBuildFuseActive.Value = VAR_SelectedSlot.Value != -1;
        if (VAR_BuildFuseProgress != null) VAR_BuildFuseProgress.Value = 1f;

        wasLookingAtGround = true;
    }

    private void OnVAR_IsBuildFuseActiveChanged()
    {
        // Можно добавить дополнительную логику при изменении режима стройки
    }

    private void OnVAR_BuildFuseProgressChanged()
    {
        // UI обновляется автоматически через SO
    }

    private void OnVAR_PickupProgressChanged()
    {
        // UI обновляется автоматически через SO
    }

    void Update()
    {
        Transform origin = cameraPrefab;
        if (origin == null)
        {
            if (Camera.main != null) origin = Camera.main.transform;
            else origin = transform;
        }
        
        // 1. ЕСЛИ МЫ НЕСЕМ КЛЕТКУ
        bool isCarrying = VAR_IsCarrying != null && VAR_IsCarrying.Value;
        if (isCarrying)
        {
            DisableBuildMode(); // Выключаем режим стройки при переноске
            return; 
        }

        // 2. ВЫБОР ПРЕДМЕТА
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeItem(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeItem(1);

        // 3. ПРИЗРАК И ТАЙМЕР
        UpdateGhostLogic(origin);

        // 4. УСТАНОВКА ПРЕДМЕТА (ЛКМ с удержанием)
        int selectedIndex = VAR_SelectedSlot != null ? VAR_SelectedSlot.Value : -1;
        HandlePlacementHold(origin, selectedIndex);

        // 5. ОТМЕНА СТРОИТЕЛЬСТВА (ПКМ или E)
        if (selectedIndex != -1 && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.E)))
        {
            DisableBuildMode();
            ResetPlaceHoldTimer();
            return; // Прерываем выполнение Update, чтобы не вызвать HandleInteraction
        }

        // 6. ВЗАИМОДЕЙСТВИЕ (E - только если не в режиме стройки)
        if (selectedIndex == -1)
        {
            HandleInteraction(origin);
        }
    }

    // ================== ЛОГИКА ПРИЗРАКОВ И ТАЙМЕРА ==================

    void UpdateGhostLogic(Transform origin)
    {
        if (placeCooldownTimer > 0f)
        {
            DestroyGhost();
            return;
        }

        int selectedIndex = VAR_SelectedSlot != null ? VAR_SelectedSlot.Value : -1;

        // Если ничего не выбрано - выходим
        if (selectedIndex == -1)
        {
            DestroyGhost();
            return;
        }

        // 1. Проверяем наличие предметов
        bool hasItem = false;
        if (selectedIndex == 0 && VAR_TrapsCount != null && VAR_TrapsCount.Value > 0) hasItem = true;
        if (selectedIndex == 1 && VAR_CamerasCount != null && VAR_CamerasCount.Value > 0) hasItem = true;

        if (!hasItem)
        {
            DisableBuildMode();
            return;
        }

        // 2. Ищем землю/дерево в зависимости от слота
        LayerMask targetLayer = (selectedIndex == 0) ? groundLayer : treeLayer;
        float currentBuildDist = (selectedIndex == 0) ? buildDistance : cameraBuildDistance;
        RaycastHit hit;
        bool isLooking = Physics.Raycast(origin.position, origin.forward, out hit, currentBuildDist, targetLayer);

        if (isLooking)
        {
            // --- МЫ СМОТРИМ НА ЗЕМЛЮ ---
            ghostTimer = ghostTimeout; // Сбрасываем таймер на максимум (5 сек)

            // UI Logic через SO
            if (VAR_IsBuildFuseActive != null) VAR_IsBuildFuseActive.Value = true;
            if (VAR_BuildFuseProgress != null) VAR_BuildFuseProgress.Value = 1f;

            // Логика создания/перемещения призрака
            GameObject neededGhostPrefab = (selectedIndex == 0) ? trapGhostPrefab : cameraGhostPrefab;

            if (currentGhost == null) currentGhost = Instantiate(neededGhostPrefab);
            else if (!currentGhost.name.Contains(neededGhostPrefab.name))
            {
                DestroyGhost();
                currentGhost = Instantiate(neededGhostPrefab);
            }

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            float ghostHeightAdjust = (selectedIndex == 0) ? trapGhostOffset : cameraGhostOffset;

            if (selectedIndex == 1) // Поворот камеры
            {
                if (cameraLookAtPlayer)
                {
                    Vector3 lookPos = origin.position - hit.point;
                    // lookPos.y = 0; // Раскомментируйте, если нужно вращение только по горизонтали
                    if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
                }
                else
                {
                    Vector3 lookPos = transform.position - hit.point;
                    lookPos.y = 0;
                    if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
                }
            }

            Vector3 position = hit.point + (hit.normal * ghostHeightAdjust);
            currentGhost.transform.position = position;
            currentGhost.transform.rotation = rotation;
        }
        else
        {
            // --- МЫ СМОТРИМ В НЕБО ---
            DestroyGhost(); // Прячем визуал

            // UI Logic: Отвели взгляд — фитиль укорачивается по таймеру
            float p = Mathf.Clamp01(ghostTimer / ghostTimeout);
            if (VAR_IsBuildFuseActive != null) VAR_IsBuildFuseActive.Value = true;
            if (VAR_BuildFuseProgress != null) VAR_BuildFuseProgress.Value = p;

            // Уменьшаем таймер
            ghostTimer -= Time.deltaTime;
            if (ghostTimer <= 0)
            {
                DisableBuildMode(); // Время вышло - отключаем режим
            }
        }

        wasLookingAtGround = isLooking;
    }

    void ChangeItem(int index)
    {
        if (VAR_SelectedSlot != null) VAR_SelectedSlot.Value = index;
        // OnVAR_SelectedSlotChanged() вызывается автоматически благодаря [Bind]
    }

    void DisableBuildMode()
    {
        if (VAR_SelectedSlot != null) VAR_SelectedSlot.Value = -1;
        // OnVAR_SelectedSlotChanged() вызывается автоматически благодаря [Bind]
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
        int selectedIndex = VAR_SelectedSlot != null ? VAR_SelectedSlot.Value : -1;
        LayerMask targetLayer = (selectedIndex == 0) ? groundLayer : treeLayer;
        float currentBuildDist = (selectedIndex == 0) ? buildDistance : cameraBuildDistance;

        if (Physics.Raycast(origin.position, origin.forward, out hit, currentBuildDist, targetLayer))
        {
            bool canPlace = false;
            GameObject objectToSpawn = null;
            float currentRealDepth = 0f;


            if (selectedIndex == 0)
            {
                if (VAR_TrapsCount != null && VAR_TrapsCount.Value > 0)
                {
                    VAR_TrapsCount.ApplyChange(-1);
                    canPlace = true;
                    objectToSpawn = trapPrefab;
                    currentRealDepth = trapEmbedDepth;
                }
            }
            else if (selectedIndex == 1)
            {
                if (VAR_CamerasCount != null && VAR_CamerasCount.Value > 0)
                {
                    VAR_CamerasCount.ApplyChange(-1);
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentRealDepth = cameraEmbedDepth;
                }
            }

            if (canPlace && objectToSpawn != null)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (selectedIndex == 1)
                {
                    if (cameraLookAtPlayer)
                    {
                        Vector3 lookPos = origin.position - hit.point;
                        // lookPos.y = 0; // Раскомментируйте, если нужно вращение только по горизонтали
                        if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
                    }
                    else
                    {
                        Vector3 lookPos = transform.position - hit.point;
                        lookPos.y = 0;
                        if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
                    }
                }

                Vector3 position = hit.point - hit.normal * currentRealDepth;
                Instantiate(objectToSpawn, position, rotation);

                if (dustEffectPrefab != null)
                {
                    float dustOffset = (selectedIndex == 0) ? trapDustOffset : cameraDustOffset;
                    Vector3 dustPos = hit.point + (hit.normal * dustOffset);
                    Instantiate(dustEffectPrefab, dustPos, Quaternion.LookRotation(hit.normal));
                }
                
                // После установки таймер обновляем, чтобы можно было ставить дальше
                ghostTimer = ghostTimeout;
            }
        }
    }

    // ================== ЛОГИКА ВЗАИМОДЕЙСТВИЯ (E) ==================

    void HandleInteraction(Transform origin)
    {
        RaycastHit hit;
        bool lookingAtPickupable = false;

        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            // Ищем компоненты ловушек через интерфейс для модульности
            IInteractableTrap trap = hit.collider.GetComponentInParent<IInteractableTrap>();
            if (trap == null) trap = hit.collider.GetComponentInChildren<IInteractableTrap>();

            if (trap != null && trap.CanBePickedUp)
            {
                lookingAtPickupable = true;
                if (Input.GetKey(KeyCode.E))
                {
                    carrier.ProcessHold(hit.collider.gameObject);
                }
                return;
            }

            // Проверяем камеру (отдельно, т.к. это не ловушка)
            SecurityCameraSetup camera = hit.collider.GetComponentInParent<SecurityCameraSetup>();
            if (camera == null) camera = hit.collider.GetComponentInChildren<SecurityCameraSetup>();

            if (camera != null)
            {
                lookingAtPickupable = true;
                if (Input.GetKey(KeyCode.E))
                {
                    carrier.ProcessHold(hit.collider.gameObject);
                }
                return;
            }
            else
            {
                // Лог для дебага, если мы навели на что-то на слое Interact, но это не ловушка
                // Debug.Log($"[Interaction] Hit {hit.collider.name}, but no Trap or Camera found.");
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null)
                {
                    interactable.Interact();
                    return;
                }

                MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
                if (monitor != null && CctvManager.instance != null && !CctvManager.instance.isMonitorActive)
                {
                    CctvManager.instance.EnterMonitorMode(); return;
                }
            }
        }

        if (!lookingAtPickupable) carrier.ResetHoldTimer();
    }

    // ================== ЛОГИКА УДЕРЖАНИЯ ЛКМ ДЛЯ УСТАНОВКИ ==================

    void HandlePlacementHold(Transform origin, int selectedIndex)
    {
        if (placeCooldownTimer > 0f)
        {
            placeCooldownTimer -= Time.deltaTime;
            ResetPlaceHoldTimer();
            return;
        }

        // Если ничего не выбрано или нет призрака - сбрасываем таймер
        if (selectedIndex == -1 || currentGhost == null)
        {
            ResetPlaceHoldTimer();
            return;
        }

        // Проверяем что мы смотрим на нужный слой (земля или дерево)
        LayerMask targetLayer = (selectedIndex == 0) ? groundLayer : treeLayer;
        float currentBuildDist = (selectedIndex == 0) ? buildDistance : cameraBuildDistance;
        RaycastHit hit;
        bool isLookingAtTarget = Physics.Raycast(origin.position, origin.forward, out hit, currentBuildDist, targetLayer);

        if (isLookingAtTarget && Input.GetMouseButton(0)) // ЛКМ зажата
        {
            // Увеличиваем таймер
            placeHoldTimer += Time.deltaTime;

            // Обновляем бар прогресса
            if (VAR_PickupProgress != null)
                VAR_PickupProgress.Value = placeHoldTimer / placeHoldTimeRequired;

            // Если удержали достаточно - устанавливаем
            if (placeHoldTimer >= placeHoldTimeRequired)
            {
                TryPlaceItem(origin);
                ResetPlaceHoldTimer();
                placeCooldownTimer = placeCooldownSeconds;
            }
        }
        else
        {
            // Если отпустили ЛКМ или смотрим не на землю - сбрасываем
            if (!Input.GetMouseButton(0))
            {
                ResetPlaceHoldTimer();
            }
        }
    }

    void ResetPlaceHoldTimer()
    {
        placeHoldTimer = 0f;
        if (VAR_PickupProgress != null) VAR_PickupProgress.Value = 0f;
    }
}
