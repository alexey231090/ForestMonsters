using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Variables/Vector3")]
public class Vector3Variable : ScriptableVariable<Vector3>
{
    public void ApplyChange(Vector3 amount) => Value += amount;
}
