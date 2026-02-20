using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Vector3")]
public class Vector3Variable : ScriptableObject
{
    [Header("Settings")]
    public Vector3 InitialValue;

    [System.NonSerialized]
    public Vector3 Value;

    private void OnEnable()
    {
        Value = InitialValue;
    }

    public void SetValue(Vector3 value) => Value = value;
    public void ApplyChange(Vector3 amount) => Value += amount;
}
