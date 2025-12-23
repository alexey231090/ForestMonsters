using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2, 5)] public string description = "Интеракция игрока. Тратит предметы из инвентаря GameManager при установке.";

    [Header("Settings")]
    public float interactDistance = 4f;
    public LayerMask interactLayer;

    [Header("Prefabs")]
    public GameObject trapPrefab;
    public GameObject cameraItemPrefab;

    [Header("References")]
    public Transform cameraPrefab;
    public CctvManager cctvManager;

    [Header("Placement Settings (Offsets)")]
    public float trapEmbedDepth = 0f;   // Настройка высоты для ЛОВУШКИ
    public float cameraEmbedDepth = 0f; // Настройка высоты для КАМЕРЫ

    private int selectedItemIndex = 0; // 0 = Trap, 1 = Camera

    void Update()
    {
        var origin = cameraPrefab != null ? cameraPrefab : (Camera.main != null ? Camera.main.transform : transform);
        Debug.DrawRay(origin.position, origin.forward * interactDistance, Color.red);

        // --- ВЫБОР ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedItemIndex = 0;
            Debug.Log($"Выбрана: ЛОВУШКА (У вас: {GameManager.instance.trapsCount})");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedItemIndex = 1;
            Debug.Log($"Выбрана: КАМЕРА (У вас: {GameManager.instance.camerasCount})");
        }

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

    void TryPlaceItem(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance, interactLayer))
        {
            // Проверка наличия ресурсов перед установкой
            bool canPlace = false;
            GameObject objectToSpawn = null;
            float currentEmbedDepth = 0f; // Временная переменная для текущей высоты

            if (selectedItemIndex == 0) // Ловушка
            {
                if (GameManager.instance.TryUseTrap())
                {
                    canPlace = true;
                    objectToSpawn = trapPrefab;
                    currentEmbedDepth = trapEmbedDepth; // Используем настройку ловушки
                }
                else
                {
                    Debug.Log("Нет ловушек! Купите в магазине (Монитор).");
                }
            }
            else if (selectedItemIndex == 1) // Камера
            {
                if (GameManager.instance.TryUseCamera())
                {
                    canPlace = true;
                    objectToSpawn = cameraItemPrefab;
                    currentEmbedDepth = cameraEmbedDepth; // Используем настройку камеры
                }
                else
                {
                    Debug.Log("Нет камер! Купите в магазине (Монитор).");
                }
            }

            // Если ресурс есть, ставим объект
            if (canPlace && objectToSpawn != null)
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                if (selectedItemIndex == 1)
                {
                    Vector3 lookPos = transform.position - hit.point;
                    lookPos.y = 0;
                    if (lookPos != Vector3.zero) rotation = Quaternion.LookRotation(lookPos);
                }

                // Используем правильную глубину для конкретного предмета
                Vector3 position = hit.point - hit.normal * currentEmbedDepth;
                
                Instantiate(objectToSpawn, position, rotation);
            }
        }
    }

    void TryInteract(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance))
        {
            // 1. ЛОВУШКА (С поддержкой TrapBox, как у тебя было)
            // Пытаемся найти компонент на самом объекте или в родителях
            TrapBox trapbox = hit.collider.GetComponentInParent<TrapBox>(); 
            // Если TrapBox не найден, ищем просто Trap
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

            // 2. МОНИТОР
            MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
            if (monitor != null)
            {
                if (CctvManager.instance != null) CctvManager.instance.EnterMonitorMode();
                return;
            }

            // 3. КРОВАТЬ
            BedTrigger bed = hit.collider.GetComponent<BedTrigger>();
            if (bed != null)
            {
                if (GameManager.instance != null) GameManager.instance.SkipCurrentPhase();
                return;
            }

            // 4. ПЛАТФОРМА
            ParkPlatform platform = hit.collider.GetComponent<ParkPlatform>();
            if (platform != null)
            {
                platform.TryPlaceMonster();
                return;
            }
        }
    }
}