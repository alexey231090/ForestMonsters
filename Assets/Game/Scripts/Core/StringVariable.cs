using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/String")]
public class StringVariable : ScriptableObject
{
    [Header("Settings")]
    public string InitialValue;

    [System.NonSerialized]
    public string Value;

    private void OnEnable()
    {
        Value = InitialValue;
    }

    public void SetValue(string value) => Value = value;
}
