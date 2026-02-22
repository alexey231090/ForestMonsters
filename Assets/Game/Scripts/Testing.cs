using UnityEngine;

public class Testing : MonoBehaviour
{
    [SerializeField] private FloatVariable VAR_PickupProgress;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            VAR_PickupProgress.Value += 0.3f;
            
        }

    }
}
