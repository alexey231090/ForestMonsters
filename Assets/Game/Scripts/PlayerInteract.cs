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
    public float embedDepth = 0f;
    public CctvManager cctvManager;

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

            if (selectedItemIndex == 0) // Ловушка
            {
                if (GameManager.instance.TryUseTrap())
                {
                    canPlace = true;
                    objectToSpawn = trapPrefab;
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

                Vector3 position = hit.point - hit.normal * embedDepth;
                Instantiate(objectToSpawn, position, rotation);
            }
        }
    }

    void TryInteract(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance))
        {
            // 1. ЛОВУШКА
            Trap trap = hit.collider.GetComponent<Trap>();
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
