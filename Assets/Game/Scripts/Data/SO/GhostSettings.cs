using UnityEngine;

[CreateAssetMenu(fileName = "SET_MonsterGhostEffects", menuName = "Architecture/Settings/Ghost Effects")]
public class GhostSettings : ScriptableObject
{
    [Tooltip("Материал, который будет применяться к монстру в режиме перетаскивания.")]
    public Material ghostMaterial;
}
