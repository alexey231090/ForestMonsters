using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Float")]
public class FloatVariable : ScriptableVariable<float>
{
    public void ApplyChange(float amount) => Value += amount;
}
