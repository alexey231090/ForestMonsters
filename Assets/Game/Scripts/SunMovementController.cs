using UnityEngine;

public class SunMovementController : MonoBehaviour
{
    [Header("Day Rotation Settings")]
    public float dayRotationSpeedX = 0f;
    public float dayRotationSpeedY = 0f;
    public float dayRotationSpeedZ = 0f;

    [Header("Night Rotation Settings")]
    public float nightRotationSpeedX = 0f;
    public float nightRotationSpeedY = 0f;
    public float nightRotationSpeedZ = 0f;

    [Header("Rotation Targets")]
    public Vector3 dayStartRotation = new Vector3(45f, 0f, 0f);
    public Vector3 nightEndRotation = new Vector3(-45f, 0f, 0f);

    [Header("References")]
    public bool isDay = true;

    [Header("Lighting")]
    public Light sunLight;
    public Color dayFog = new Color(0.5f, 0.6f, 0.7f);
    public Color nightFog = new Color(0.02f, 0.02f, 0.05f);

    private void Start()
    {
        // Initialize sun rotation based on current phase
        transform.rotation = Quaternion.Euler(isDay ? dayStartRotation : nightEndRotation);
    }

    private void Update()
    {
        // Determine rotation speeds based on day/night phase
        float rotationSpeedX = isDay ? dayRotationSpeedX : nightRotationSpeedX;
        float rotationSpeedY = isDay ? dayRotationSpeedY : nightRotationSpeedY;
        float rotationSpeedZ = isDay ? dayRotationSpeedZ : nightRotationSpeedZ;

        // Apply rotation around each axis
        transform.Rotate(rotationSpeedX * Time.deltaTime, rotationSpeedY * Time.deltaTime, rotationSpeedZ * Time.deltaTime);
    }

    // Method to switch between day and night phases
    public void SetDayPhase(bool dayPhase)
    {
        isDay = dayPhase;
    }

    // Method to determine current phase based on sun's X rotation
    public bool IsCurrentlyDay()
    {
        float currentXRotation = transform.rotation.eulerAngles.x;
        // Normalize angle to -180 to 180 range
        if (currentXRotation > 180)
            currentXRotation -= 360;
        
        // If X rotation is less than -12, it's night; otherwise it's day
        return currentXRotation > -12;
    }

    // Public method to instantly transition to night
    public void InstantTransitionToNight()
    {
        isDay = false;
        transform.rotation = Quaternion.Euler(nightEndRotation);
    }
}