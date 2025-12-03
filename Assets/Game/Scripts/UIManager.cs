using UnityEngine;
public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    void Awake() { instance = this; }
    public void UpdateUI() { }
}
