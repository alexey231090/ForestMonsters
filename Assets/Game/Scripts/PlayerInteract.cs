using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerInteract : MonoBehaviour
{
    [Header("Settings")]
    public float interactDistance = 4f;     // Дистанция для E
    public float buildDistance = 10f;       // Дистанция для призраков (сделал побольше)
    
    public LayerMask interactLayer; // Слой предметов (ловушки, мониторы)
    
    // !!! ВАЖНО: Выбери здесь слой "Default" или "Ground" в инспекторе !!!
    public LayerMask groundLayer;   
    
    public float holdTimeRequired = 1.0f; 

    [Header("UI")]
    public Image holdProgressBar; 

    [Header("Hold Settings")]
    public Transform holdPoint; 

    [Header("Prefabs")]
    public GameObject trapPrefab;
    public GameObject cameraItemPrefab;
    public GameObject trapGhostPrefab;
    public GameObject cameraGhostPrefab;

    [Header("References, camera Player")]
    public Transform cameraPrefab;
    public CctvManager cctvManager;

    [Header("Offsets")]
    public float trapEmbedDepth = 0f;
    public float cameraEmbedDepth = 0f;
    public float trapGhostOffset = 0f;
    public float cameraGhostOffset = 0f;

    private int selectedItemIndex = 0; 
    private GameObject currentGhost;
    private Trap carriedTrap;        
    private float currentHoldTimer = 0f;

    void Start()
    {
        if (holdProgressBar) holdProgressBar.fillAmount = 0;
        if (cameraPrefab == null)
        {
            Debug.LogError("No camera prefab PlayerController found!");
        }
    }

    void Update()
    {
        var origin = cameraPrefab != null ? cameraPrefab : (Camera.main != null ? Camera.main.transform : transform);
        
        // 1. Если несем клетку - выходим, кнопки 1/2 не работают
        if (carriedTrap != null)
        {
            HandleCarrying();
            DestroyGhost(); 
            return; 
        }

        // 2. Выбор предмета (ВЕРНУЛ ЛОГИ!)
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

        // 3. Призрак и Установка
        UpdateGhost(origin);

        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceItem(origin);
        }

        // 4. Взаимодействие E
        HandleInteraction(origin);
    }

    // --- ЛОГИКА ПРИЗРАКОВ ---

    void UpdateGhost(Transform origin)
    {
        // Проверка наличия
        bool hasItem = false;
        if (selectedItemIndex == 0 && GameManager.instance.trapsCount > 0) hasItem = true;
        if (selectedItemIndex == 1 && GameManager.instance.camerasCount > 0) hasItem = true;

        if (!hasItem) { DestroyGhost(); return; }

        RaycastHit hit;
        
        // ИСПРАВЛЕНИЕ: Используем groundLayer для поиска земли
        if (Physics.Raycast(origin.position, origin.forward, out hit, buildDistance, groundLayer))
        {
            GameObject neededGhostPrefab = (selectedItemIndex == 0) ? trapGhostPrefab : cameraGhostPrefab;

            if (currentGhost == null) currentGhost = Instantiate(neededGhostPrefab);
            else if (!currentGhost.name.Contains(neededGhostPrefab.name))
            {
                DestroyGhost();
                currentGhost = Instantiate(neededGhostPrefab);
            }

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

    // --- ЛОГИКА УСТАНОВКИ ---

    void TryPlaceItem(Transform origin)
    {
        RaycastHit hit;
        // ИСПРАВЛЕНИЕ: Тоже используем groundLayer
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
                else Debug.Log("Нет ловушек!");
            }
            else if (selectedItemIndex == 1)
            {
                if (GameManager.instance.TryUseCamera())
                {
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentRealDepth = cameraEmbedDepth;
                }
                else Debug.Log("Нет камер!");
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
            }
        }
    }

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

    // --- ВЗАИМОДЕЙСТВИЕ И ПЕРЕНОСКА (Остается как было) ---

    void HandleInteraction(Transform origin)
    {
        RaycastHit hit;
        bool lookingAtTrap = false;

        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            TrapBox trapbox = hit.collider.GetComponentInParent<TrapBox>();
            Trap trap = hit.collider.GetComponent<Trap>();
            if (trapbox != null) trap = trapbox.GetComponentInChildren<Trap>();

            if (trap != null)
            {
                lookingAtTrap = true;
                if (Input.GetKey(KeyCode.E))
                {
                    currentHoldTimer += Time.deltaTime;
                    if (holdProgressBar) holdProgressBar.fillAmount = currentHoldTimer / holdTimeRequired;
                    if (currentHoldTimer >= holdTimeRequired)
                    {
                        PickUpTrap(trap);
                        ResetHold();
                    }
                }
                else ResetHold();
                return; 
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
                if (monitor != null && CctvManager.instance != null) { CctvManager.instance.EnterMonitorMode(); return; }

                BedTrigger bed = hit.collider.GetComponent<BedTrigger>();
                if (bed != null && GameManager.instance != null) { GameManager.instance.SkipCurrentPhase(); return; }

                ParkPlatform platform = hit.collider.GetComponent<ParkPlatform>();
                if (platform != null) { platform.TryPlaceMonster(); return; }
            }
        }

        if (!lookingAtTrap) ResetHold();
    }

    void PickUpTrap(Trap trap)
    {
        carriedTrap = trap;
        var rb = trap.GetComponentInParent<Rigidbody>();
        if (rb) rb.isKinematic = true;
        Collider[] cols = trap.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;
        
        Transform targetTransform = trap.trapbox != null ? trap.trapbox.transform : trap.transform;
        targetTransform.SetParent(holdPoint);
        targetTransform.DOLocalMove(Vector3.zero, 0.5f);
        targetTransform.DOLocalRotate(Vector3.zero, 0.5f);
    }

    void HandleCarrying()
    {
        if (holdProgressBar) holdProgressBar.fillAmount = 0; 
        if (Input.GetKeyDown(KeyCode.E)) TryDropTrap();
    }

    void TryDropTrap()
    {
        RaycastHit hit;
        // Используем GroundLayer для поиска земли
        if (Physics.Raycast(holdPoint.position, Vector3.down, out hit, 10f, groundLayer))
        {
            DropTrap(hit.point);
        }
    }

    void DropTrap(Vector3 floorPos)
    {
        Trap trapToDrop = carriedTrap;
        Transform targetTransform = trapToDrop.trapbox != null ? trapToDrop.trapbox.transform : trapToDrop.transform;
        targetTransform.SetParent(null);
        targetTransform.DOMove(floorPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            if (trapToDrop != null)
            {
                Collider[] cols = trapToDrop.GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = true;
                var rb = targetTransform.GetComponent<Rigidbody>();
                if (rb) rb.isKinematic = false;
            }
        });
        Quaternion targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        targetTransform.DORotateQuaternion(targetRot, 0.5f);
        carriedTrap = null;
    }

    void ResetHold()
    {
        currentHoldTimer = 0f;
        if (holdProgressBar) holdProgressBar.fillAmount = 0;
    }
}