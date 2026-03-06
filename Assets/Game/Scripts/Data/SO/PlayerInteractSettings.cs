using UnityEngine;

[CreateAssetMenu(fileName = "SET_PlayerInteract", menuName = "Architecture/Settings/Player Interact Settings")]
public class PlayerInteractSettings : ScriptableObject
{
    [Header("Interaction Distances")]
    public float interactDistance = 8f;         // Для ловушек и прочих объектов
    public float cameraInteractDistance = 12f;   // Отдельно для камер

    [Header("Building Distances")]
    public float buildDistance = 10f;
    public float cameraBuildDistance = 15f;

    [Header("Placement Hold Settings")]
    public float placeHoldTimeRequired = 0.5f;
    public float placeCooldownSeconds = 2.0f;

    [Header("Ghost & Misc")]
    public float ghostTimeout = 5.0f;
    public bool cameraLookAtPlayer = true;

    [Header("Placement Offsets")]
    public float trapEmbedDepth = 0f;
    public float cameraEmbedDepth = 0f;
    public float trapGhostOffset = 0f;
    public float cameraGhostOffset = 0f;

    [Header("VFX Offsets")]
    public float trapDustOffset = 0.1f;
    public float cameraDustOffset = 0.1f;
}
