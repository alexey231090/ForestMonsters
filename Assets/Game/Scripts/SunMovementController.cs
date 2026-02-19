using UnityEngine;

public class SunMovementController : MonoBehaviour
{
    [Header("Events")]
    [SerializeField] private GameEvent call_onDayStarted;
    [SerializeField] private GameEvent call_onNightStarted;

    [Header("Settings")]
    [SerializeField] private SunSettings settings;

    [Header("State (Read Only)")]
    public bool isNight = false;
    [SerializeField] private float currentPhaseTimer = 0f;

    [Header("Visuals (Reference)")]
    public Light sunLight;

    private void Start()
    {
        // Инициализируем визуализацию при старте
        if (isNight) SetVisualsForNight();
        else SetVisualsForDay();
    }

    private void Update()
    {
        HandleTimeCycle();
    }

    private void HandleTimeCycle()
    {
        if (settings == null) return;
        currentPhaseTimer += Time.deltaTime;

        float currentDuration = isNight ? settings.nightDurationMinutes : settings.dayDurationMinutes;
        float durationInSeconds = currentDuration * 60f;

        // Рассчитываем прогресс от 0.0 до 1.0 (0% -> 100% фазы)
        float progress = Mathf.Clamp01(currentPhaseTimer / durationInSeconds);

        // Обновляем визуальное положение солнца
        UpdateSunPosition(progress, isNight);

        // Если время вышло — меняем фазу
        if (currentPhaseTimer >= durationInSeconds)
        {
            TogglePhase();
        }
    }

    public void TogglePhase()
    {
        if (isNight) StartDay();
        else StartNight();
    }

    private void StartDay()
    {
        isNight = false;
        currentPhaseTimer = 0f;
        SetVisualsForDay();
        Debug.Log(">>> SunMovement: НАСТУПИЛ ДЕНЬ");
    }

    private void StartNight()
    {
        isNight = true;
        currentPhaseTimer = 0f;
        SetVisualsForNight();
        Debug.Log(">>> SunMovement: НАСТУПИЛА НОЧЬ");
    }

    // Раньше вызывался из GameManager, теперь внутренний
    private void UpdateSunPosition(float progress, bool isNight)
    {
        if (settings == null) return;
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

        Quaternion rotX = Quaternion.Euler(xAngle, 0f, 0f);
        Quaternion rotTilt = Quaternion.Euler(0f, 0f, settings.sunTrajectoryTilt);
        Quaternion rotY = Quaternion.Euler(0f, settings.sunDirectionY, 0f);

        transform.rotation = rotY * rotTilt * rotX;
    }

    public void SetVisualsForDay()
    {
        if (settings == null) return;
        RenderSettings.fogColor = settings.dayFog;
        RenderSettings.ambientIntensity = settings.dayIntensity;
        if (sunLight) sunLight.intensity = settings.dayIntensity;

        if (call_onDayStarted != null) call_onDayStarted.Raise();
    }

    public void SetVisualsForNight()
    {
        if (settings == null) return;
        RenderSettings.fogColor = settings.nightFog;
        RenderSettings.ambientIntensity = settings.nightIntensity;
        if (sunLight) sunLight.intensity = settings.nightIntensity;

        if (call_onNightStarted != null) call_onNightStarted.Raise();
    }
}
