using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Int")]
public class IntVariable : ScriptableVariable<int>
{
    public void ApplyChange(int amount) => Value += amount;
}
