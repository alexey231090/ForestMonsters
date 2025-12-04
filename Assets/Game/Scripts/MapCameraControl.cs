using UnityEngine;

public class MapCameraControl : MonoBehaviour
{
    [Header("Settings")]
    public float panSpeed = 20f;       // Скорость перемещения
    public float zoomSpeed = 50f;      // Скорость зума
    public float minZoom = 10f;        // Максимальное приближение
    public float maxZoom = 100f;       // Максимальное отдаление

    // Границы карты (чтобы камера не улетала в пустоту)
    public Vector2 mapLimit = new Vector2(100f, 100f);

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void Update()
    {
        // Работаем только если камера включена
        if (!cam.enabled) return;

        // 1. ПЕРЕМЕЩЕНИЕ (Мышкой с зажатой ЛКМ или ПКМ)
        // Можно и WASD, но у нас руки на мышке
        if (Input.GetMouseButton(0))
        {
            float h = -Input.GetAxis("Mouse X") * panSpeed * Time.deltaTime; // Минус для инверсии (тянем карту)
            float v = -Input.GetAxis("Mouse Y") * panSpeed * Time.deltaTime;

            // Двигаем камеру. Так как она повернута вниз, двигаем по осям X и Y локально
            transform.Translate(h, v, 0);
        }

        // 2. ЗУМ (Колесико)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            cam.orthographicSize -= scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
        }

        // 3. ОГРАНИЧЕНИЕ ГРАНИЦ (Чтобы не улететь)
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -mapLimit.x, mapLimit.x);
        pos.z = Mathf.Clamp(pos.z, -mapLimit.y, mapLimit.y);
        // Y не трогаем, это высота
        transform.position = pos;
    }
}