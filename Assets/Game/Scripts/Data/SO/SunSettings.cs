using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Settings/Sun Settings")]
public class SunSettings : ScriptableObject
{
    [Header("Time")]
    [Tooltip("Длительность дня в минутах")]
    public float dayDurationMinutes = 1f;
    [Tooltip("Длительность ночи в минутах")]
    public float nightDurationMinutes = 1f;

    [Header("Rotation")]
    [Tooltip("Поворот солнца по горизонту (Ось Y)")]
    [Range(0f, 360f)]
    public float sunDirectionY = 45f;
    
    [Tooltip("Наклон траектории солнца (Ось Z)")]
    [Range(-90f, 90f)]
    public float sunTrajectoryTilt = 0f;

    [Header("Visuals (Fog)")]
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    [Header("Intensity")]
    public float dayIntensity = 1f;
    public float nightIntensity = 0.1f;
}
