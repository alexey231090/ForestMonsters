using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Int")]
public class IntVariable : ScriptableObject
{
    [Header("Settings")]
    public int InitialValue;

    [System.NonSerialized]
    public int Value;

    private void OnEnable()
    {
        Value = InitialValue;
    }

    public void SetValue(int value) => Value = value;
    public void ApplyChange(int amount) => Value += amount;
}
