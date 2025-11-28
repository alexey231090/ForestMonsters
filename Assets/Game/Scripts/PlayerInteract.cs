using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2,5)] public string description = "Интеракция игрока: установка ловушек/камер (ЛКМ) и взаимодействие (E) с ловушкой и монитором.";
    [Header("Settings")]
    public float interactDistance = 4f;
    public LayerMask interactLayer;

    [Header("Prefabs")]
    public GameObject trapPrefab;       // Префаб ловушки
    public GameObject cameraItemPrefab; // Префаб камеры (на шесте)

    [Header("References")]
    public Transform cameraPrefab;
    public float embedDepth = 0f;

    // Исправили тип на CctvManager
    public CctvManager cctvManager;

    // 0 = Ловушка, 1 = Камера
    private int selectedItemIndex = 0;

    void Update()
    {
        var origin = cameraPrefab != null ? cameraPrefab : (Camera.main != null ? Camera.main.transform : transform);
        Debug.DrawRay(origin.position, origin.forward * interactDistance, Color.red);

        // --- ВЫБОР ПРЕДМЕТА (1 и 2) ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            selectedItemIndex = 0;
            Debug.Log("Выбрана: ЛОВУШКА");
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            selectedItemIndex = 1;
            Debug.Log("Выбрана: КАМЕРА");
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
            // Выбираем, что спавнить
            GameObject objectToSpawn = (selectedItemIndex == 0) ? trapPrefab : cameraItemPrefab;

            if (objectToSpawn == null) return;

            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

            // Если ставим камеру, поворачиваем её к игроку
            if (selectedItemIndex == 1)
            {
                Vector3 lookPos = transform.position - hit.point;
                lookPos.y = 0;
                if (lookPos != Vector3.zero)
                    rotation = Quaternion.LookRotation(lookPos);
            }

            Vector3 position = hit.point - hit.normal * embedDepth;
            Instantiate(objectToSpawn, position, rotation);
        }
    }

    void TryInteract(Transform origin)
    {
        RaycastHit hit;
        if (Physics.Raycast(origin.position, origin.forward, out hit, interactDistance))
        {
            // 1. Проверка ЛОВУШКИ
            Trap trap = hit.collider.GetComponent<Trap>();
            if (trap != null)
            {
                if (trap.HasCatch()) trap.CollectPrey();
                return;
            }

            // 2. Проверка МОНИТОРА
            MonitorTrigger monitor = hit.collider.GetComponent<MonitorTrigger>();
            if (monitor != null)
            {
                if (CctvManager.instance != null)
                {
                    if (!CctvManager.instance.IsMonitorActive())
                        CctvManager.instance.EnterMonitorMode();
                }
                else if (cctvManager != null)
                {
                    if (!cctvManager.IsMonitorActive())
                        cctvManager.EnterMonitorMode();
                }
            }

            // 3. КРОВАТЬ
            BedTrigger bed = hit.collider.GetComponent<BedTrigger>();
            if (bed != null)
            {
                if (GameManager.instance != null)
                {
                    Debug.Log("Ложимся спать... Время перематывается.");
                    GameManager.instance.SkipCurrentPhase();
                }
                return;
            }
        }
    }
}
