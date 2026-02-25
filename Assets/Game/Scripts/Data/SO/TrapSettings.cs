using UnityEngine;

/// <summary>
/// Настройки ловушки Trap2.
/// Вынесены в ScriptableObject для гибкой настройки без изменения кода.
/// </summary>
[CreateAssetMenu(menuName = "Architecture/Settings/Trap Settings")]
public class TrapSettings : ScriptableObject
{
    [Header("Настройки Сферы Обнаружения")]
    [Tooltip("Радиус сферы обнаружения врагов и триггеров парка")]
    public float detectionRadius = 1.0f;
    
    [Tooltip("Смещение центра сферы относительно позиции ловушки")]
    public Vector3 sphereOffset = Vector3.up * 0.5f;
    
    [Tooltip("Слои для обнаружения (враги, триггеры парка)")]
    public LayerMask detectionLayer;
    
    [Tooltip("Интервал проверки сферы в секундах (0.1 - оптимально)")]
    [Range(0.05f, 1f)]
    public float checkInterval = 0.1f;

    [Header("Настройки Захвата")]
    [Tooltip("Скорость притягивания врага к точке захвата")]
    [Range(0.1f, 2f)]
    public float attractionSpeed = 0.5f;

    [Header("Настройки Переноски")]
    [Tooltip("Время анимации поднятия ловушки")]
    [Range(0.1f, 1f)]
    public float pickUpDuration = 0.3f;
    
    [Tooltip("Время анимации установки ловушки")]
    [Range(0.1f, 1f)]
    public float dropDuration = 0.5f;

    [Header("Визуал")]
    [Tooltip("Цвет Gizmos сферы когда ловушка ищет врага")]
    public Color gizmoColorSearching = new Color(0, 1, 0, 0.3f);
    
    [Tooltip("Цвет Gizmos сферы когда враг пойман")]
    public Color gizmoColorCaught = new Color(1, 0, 0, 0.3f);
}
