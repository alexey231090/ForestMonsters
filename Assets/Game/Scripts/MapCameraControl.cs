using UnityEngine;

public class MapCameraControl : SignalBinder
{
    [Header("Settings")]
    public float panSpeed = 20f;       // Скорость перемещения
    public float zoomSpeed = 50f;      // Скорость зума
    public float minZoom = 10f;        // Максимальное приближение
    public float maxZoom = 100f;       // Максимальное отдаление

    // Границы карты (относительно начальной позиции камеры)
    [Header("Map Limits")]
    [Tooltip("Ограничение по оси X (влево/вправо от старта)")]
    public float limitX = 100f;
    [Tooltip("Ограничение по оси Z (вверх/вниз от старта)")]
    public float limitZ = 100f;

    [Header("Map Visuals")]
    public Light mapVisualLight;           // Ссылка на ДОПОЛНИТЕЛЬНЫЙ свет для карты
    
    [Header("Subscribed Events")]
    [SerializeField] private GameEvent GET_onMapEntered;
    [SerializeField] private GameEvent GET_onMapExited;

    private Camera cam;
    private Vector3 initialPosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        initialPosition = transform.position; // Запоминаем точку старта

        // Подписываемся на события через SignalBinder
        Bind(GET_onMapEntered, EnableMapLighting);
        Bind(GET_onMapExited, DisableMapLighting);

        // По умолчанию свет выключен
        if (mapVisualLight != null) mapVisualLight.enabled = false;
    }

    private Vector2 externalInput;

    /// <summary>
    /// Установка ввода от UI кнопок (вызывается из MapUIHandler)
    /// </summary>
    public void SetExternalInput(Vector2 input)
    {
        externalInput = input;
    }

    void Update()
    {
        // Работаем только если камера включена       
        if (!cam.enabled) return;

        float h = 0f;
        float v = 0f;

        // 1. ПЕРЕМЕЩЕНИЕ

        // А) Мышкой (Drag)
        if (Input.GetMouseButton(0))
        {
            h += -Input.GetAxis("Mouse X"); // Инверсия для драга
            v += -Input.GetAxis("Mouse Y");
        }

        // Б) Клавиатура (WASD / Стрелки)
        h += Input.GetAxis("Horizontal");
        v += Input.GetAxis("Vertical");

        // В) UI Кнопки (из MapUIHandler).
        h += externalInput.x;
        v += externalInput.y;

        // Применяем перемещение
        if (h != 0 || v != 0)
        {
             // Двигаем по X и Y локально (камера смотрит вниз, так что это работает как надо)
             Vector3 move = new Vector3(h, v, 0) * panSpeed * Time.deltaTime;
             transform.Translate(move);
        }

        // 2. ЗУМ (Колесико)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        // 3. ОГРАНИЧЕНИЕ ГРАНИЦ (Относительно старта)
        Vector3 pos = transform.position;
        
        // Считаем границы от начальной точки
        float minX = initialPosition.x - limitX;
        float maxX = initialPosition.x + limitX;
        float minZ = initialPosition.z - limitZ;
        float maxZ = initialPosition.z + limitZ;

        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
        
        // Y не трогаем, это высота
        transform.position = pos;
    }
    // --- УПРАВЛЕНИЕ СВЕТОМ КАРТЫ ---
    
    private void EnableMapLighting()
    {
        if (mapVisualLight != null) mapVisualLight.enabled = true;
    }

    private void DisableMapLighting()
    {
        if (mapVisualLight != null) mapVisualLight.enabled = false;
    }
}
