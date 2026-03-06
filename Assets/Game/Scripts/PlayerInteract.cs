using UnityEngine;

[RequireComponent(typeof(PlayerCarrier))]
public class PlayerInteract : SignalBinder
{
    [Header("Description")]
    [TextArea(2, 5)] public string description = "Интеракция: Установка (ЛКМ), Призраки с автоотключением, Взаимодействие (E).";

    [Header("Settings Asset")]
    public PlayerInteractSettings settings;

    [Header("Layers")]
    public LayerMask interactLayer; // Слой предметов (ловушки, мониторы)
    public LayerMask cameraLayer;   // Слой камер (ОТДЕЛЬНАЯ ДИСТАНЦИЯ)
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

    [Header("Variables SO")]
    [SerializeField] IntVariable VAR_TrapsCount;
    [SerializeField] IntVariable VAR_CamerasCount;
    [SerializeField, Bind] IntVariable VAR_SelectedSlot;
    [SerializeField, Bind] FloatVariable VAR_BuildFuseProgress;
    [SerializeField, Bind] BoolVariable VAR_IsBuildFuseActive;
    [SerializeField] BoolVariable VAR_IsCarrying;
    [SerializeField, Bind] FloatVariable VAR_PickupProgress;

    [Header("Placement Hold Settings")]
    [SerializeField] private float placeCooldownSeconds_Unused; 

    // --- ВНУТРЕННИЕ ПЕРЕМЕННЫЕ ---
    private GameObject currentGhost;
    private float ghostTimer = 0f; // Текущий таймер жизни призрака
    private bool wasLookingAtGround = false;
    private float placeHoldTimer = 0f; // Таймер удержания ЛКМ для установки
    private float placeCooldownTimer = 0f; // Таймер задержки между установками
    
    // --- HIGHLIGHTING ---
    private GameObject lastHighlightedObject;
    private struct HighlightData
    {
        public Renderer renderer;
        public Material[] originalMaterials;
    }
    private System.Collections.Generic.List<HighlightData> currentHighlights = new System.Collections.Generic.List<HighlightData>();

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
        if (settings != null) ghostTimer = settings.ghostTimeout;
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
            ClearHighlight();
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

        // 6. ВЗАИМОДЕЙСТВИЕ И ПОДСВЕТКА (E - только если не в режиме стройки)
        if (selectedIndex == -1 && settings != null)
        {
            GameObject interactable = FindInteractable(origin);
            UpdateHighlight(interactable);
            
            if (interactable != null)
            {
                HandleInteraction(origin, interactable);
            }
            else
            {
                carrier.ResetHoldTimer();
            }
        }
        else
        {
            ClearHighlight();
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
        float currentBuildDist = (selectedIndex == 0) ? settings.buildDistance : settings.cameraBuildDistance;
        RaycastHit hit;
        bool isLooking = Physics.Raycast(origin.position, origin.forward, out hit, currentBuildDist, targetLayer);

        if (isLooking)
        {
            // --- МЫ СМОТРИМ НА ЗЕМЛЮ ---
            ghostTimer = settings.ghostTimeout; // Сбрасываем таймер на максимум (5 сек)

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
            float ghostHeightAdjust = (selectedIndex == 0) ? settings.trapGhostOffset : settings.cameraGhostOffset;

            if (selectedIndex == 1) // Поворот камеры
            {
                if (settings.cameraLookAtPlayer)
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
            float p = settings.ghostTimeout > 0 ? Mathf.Clamp01(ghostTimer / settings.ghostTimeout) : 0;
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
        float currentBuildDist = (selectedIndex == 0) ? settings.buildDistance : settings.cameraBuildDistance;

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
                    currentRealDepth = settings.trapEmbedDepth;
                }
            }
            else if (selectedIndex == 1)
            {
                if (VAR_CamerasCount != null && VAR_CamerasCount.Value > 0)
                {
                    VAR_CamerasCount.ApplyChange(-1);
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentRealDepth = settings.cameraEmbedDepth;
                }
            }

            if (canPlace && objectToSpawn != null)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (selectedIndex == 1)
                {
                    if (settings.cameraLookAtPlayer)
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
                    float dustOffset = (selectedIndex == 0) ? settings.trapDustOffset : settings.cameraDustOffset;
                    Vector3 dustPos = hit.point + (hit.normal * dustOffset);
                    Instantiate(dustEffectPrefab, dustPos, Quaternion.LookRotation(hit.normal));
                }
                
                // После установки таймер обновляем, чтобы можно было ставить дальше
                ghostTimer = settings.ghostTimeout;
            }
        }
    }

    // ================== ЛОГИКА ПОДСВЕТКИ И ПОИСКА ==================

    GameObject FindInteractable(Transform origin)
    {
        RaycastHit hit;

        // 1. КАМЕРЫ
        if (Physics.Raycast(origin.position, origin.forward, out hit, settings.cameraInteractDistance, cameraLayer))
        {
            SecurityCameraSetup camera = hit.collider.GetComponentInParent<SecurityCameraSetup>();
            if (camera == null) camera = hit.collider.GetComponentInChildren<SecurityCameraSetup>();
            if (camera != null) return camera.gameObject;
        }

        // 2. ОСТАЛЬНОЕ
        if (Physics.Raycast(origin.position, origin.forward, out hit, settings.interactDistance, interactLayer))
        {
            // Проверка ловушек
            IInteractableTrap trap = hit.collider.GetComponentInParent<IInteractableTrap>();
            if (trap == null) trap = hit.collider.GetComponentInChildren<IInteractableTrap>();
            if (trap != null && trap.CanBePickedUp) return ((MonoBehaviour)trap).gameObject;

            // Проверка интерфейса (ищем в родителях, чтобы найти корень объекта)
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null) return ((MonoBehaviour)interactable).gameObject;

            // Проверка монитора
            MonitorTrigger monitor = hit.collider.GetComponentInParent<MonitorTrigger>();
            if (monitor != null) return monitor.gameObject;
        }

        return null;
    }

    void UpdateHighlight(GameObject target)
    {
        if (target == lastHighlightedObject) return;

        ClearHighlight();

        if (target == null || settings.highlightMaterial == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            lastHighlightedObject = target;

            foreach (Renderer rend in renderers)
            {
                HighlightData data = new HighlightData();
                data.renderer = rend;
                data.originalMaterials = rend.sharedMaterials;

                Material[] newMaterials = new Material[data.originalMaterials.Length + 1];
                for (int i = 0; i < data.originalMaterials.Length; i++)
                {
                    newMaterials[i] = data.originalMaterials[i];
                }
                newMaterials[newMaterials.Length - 1] = settings.highlightMaterial;

                rend.sharedMaterials = newMaterials;
                currentHighlights.Add(data);
            }
        }
    }

    void ClearHighlight()
    {
        foreach (HighlightData data in currentHighlights)
        {
            if (data.renderer != null)
            {
                // Берем текущие материалы (могли измениться другими скриптами)
                Material[] currentMats = data.renderer.sharedMaterials;
                
                // Если последний материал — это наш контур, удаляем только его
                if (currentMats.Length > 0 && currentMats[currentMats.Length - 1] == settings.highlightMaterial)
                {
                    Material[] restoredMats = new Material[currentMats.Length - 1];
                    for (int i = 0; i < restoredMats.Length; i++)
                    {
                        restoredMats[i] = currentMats[i];
                    }
                    data.renderer.sharedMaterials = restoredMats;
                }
            }
        }

        currentHighlights.Clear();
        lastHighlightedObject = null;
    }

    // ================== ЛОГИКА ВЗАИМОДЕЙСТВИЯ (E) ==================

    void HandleInteraction(Transform origin, GameObject interactable)
    {
        // Камера или ловушка (удержание E для поднятия)
        SecurityCameraSetup camera = interactable.GetComponentInParent<SecurityCameraSetup>();
        if (camera == null) camera = interactable.GetComponentInChildren<SecurityCameraSetup>();

        IInteractableTrap trap = interactable.GetComponentInParent<IInteractableTrap>();
        if (trap == null) trap = interactable.GetComponentInChildren<IInteractableTrap>();

        if (camera != null || (trap != null && trap.CanBePickedUp))
        {
            if (Input.GetKey(KeyCode.E))
            {
                carrier.ProcessHold(interactable);
            }
            else
            {
                carrier.ResetHoldTimer();
            }
            return;
        }

        // Обычные интеракты (нажатие E)
        if (Input.GetKeyDown(KeyCode.E))
        {
            IInteractable interact = interactable.GetComponent<IInteractable>();
            if (interact != null)
            {
                interact.Interact();
                return;
            }

            MonitorTrigger monitor = interactable.GetComponent<MonitorTrigger>();
            if (monitor != null && CctvManager.instance != null && !CctvManager.instance.isMonitorActive)
            {
                CctvManager.instance.EnterMonitorMode();
            }
        }
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
        float currentBuildDist = (selectedIndex == 0) ? settings.buildDistance : settings.cameraBuildDistance;
        RaycastHit hit;
        bool isLookingAtTarget = Physics.Raycast(origin.position, origin.forward, out hit, currentBuildDist, targetLayer);

        if (isLookingAtTarget && Input.GetMouseButton(0)) // ЛКМ зажата
        {
            // Увеличиваем таймер
            placeHoldTimer += Time.deltaTime;

            // Обновляем бар прогресса
            if (VAR_PickupProgress != null && settings.placeHoldTimeRequired > 0)
                VAR_PickupProgress.Value = Mathf.Clamp01(placeHoldTimer / settings.placeHoldTimeRequired);

            // Если удержали достаточно - устанавливаем
            if (placeHoldTimer >= settings.placeHoldTimeRequired)
            {
                TryPlaceItem(origin);
                ResetPlaceHoldTimer();
                placeCooldownTimer = settings.placeCooldownSeconds;
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
