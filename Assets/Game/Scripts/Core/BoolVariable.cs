using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Bool")]
public class BoolVariable : ScriptableVariable<bool>
{
    public void Toggle() => Value = !Value;
}
