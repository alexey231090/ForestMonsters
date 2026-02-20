using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Bool")]
public class BoolVariable : ScriptableObject
{
    [Header("Settings")]
    public bool InitialValue;

    [System.NonSerialized]
    public bool Value;

    private void OnEnable()
    {
        Value = InitialValue;
    }

    public void SetValue(bool value) => Value = value;
    public void Toggle() => Value = !Value;
}
