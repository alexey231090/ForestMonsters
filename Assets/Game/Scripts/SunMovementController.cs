using UnityEngine;

public class SunMovementController : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Поворот солнца по горизонту (Ось Y). Меняй это, чтобы солнце вставало с другой стороны.")]
    [Range(0f, 360f)]
    public float sunDirectionY = 45f;

    [Tooltip("Наклон траектории солнца (Ось Z). Позволяет сделать путь солнца не вертикальным, а под наклоном.")]
    [Range(-90f, 90f)]
    public float sunTrajectoryTilt = 0f;

    [Header("Visuals")]
    public Light sunLight;
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    [Header("Intensity")]
    public float dayIntensity = 1f;
    public float nightIntensity = 0.1f; // Лунный свет

    // Вызывается каждый кадр из GameManager
    public void UpdateSunPosition(float progress, bool isNight)
    {
        float xAngle;

        if (!isNight)
        {
            // ДЕНЬ: Солнце идет от 0 до 180 градусов
            xAngle = Mathf.Lerp(0f, 180f, progress);
        }
        else
        {
            // НОЧЬ: Солнце идет от 180 до 360 градусов
            xAngle = Mathf.Lerp(180f, 360f, progress);
        }

        // Применяем вращение:
        // 1. Вращение по времени суток (X)
        // 2. Наклон траектории (Z) - то, что просили добавить
        // 3. Поворот по сторонам света (Y)
        
        Quaternion rotX = Quaternion.Euler(xAngle, 0f, 0f);
        Quaternion rotTilt = Quaternion.Euler(0f, 0f, sunTrajectoryTilt);
        Quaternion rotY = Quaternion.Euler(0f, sunDirectionY, 0f);

        transform.rotation = rotY * rotTilt * rotX;
    }

    public void SetVisualsForDay()
    {
        RenderSettings.fogColor = dayFog;
        RenderSettings.ambientIntensity = dayIntensity;
        if (sunLight) sunLight.intensity = dayIntensity;
    }

    public void SetVisualsForNight()
    {
        RenderSettings.fogColor = nightFog;
        RenderSettings.ambientIntensity = nightIntensity;
        if (sunLight) sunLight.intensity = nightIntensity;
    }
}