using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Float")]
public class FloatVariable : ScriptableObject
{
    [Header("Settings")]
    public float InitialValue; // Value at startup (e.g. 100)

    [System.NonSerialized]
    public float Value; // Current runtime value

    // When the game starts or SO loads
    private void OnEnable()
    {
        Value = InitialValue; // Reset current value to initial value
    }

    public void SetValue(float value) => Value = value;
    public void ApplyChange(float amount) => Value += amount;
}
