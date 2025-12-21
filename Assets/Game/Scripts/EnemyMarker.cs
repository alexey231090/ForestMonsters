using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
    
    [Header("Description")]
    [TextArea(2,5)] public string description = "Настройка высоты делается в *CamTrigger)камере ";
     void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<PlayerInteract>() != null)
        {
            Destroy(gameObject);
        }

		
    }
    
}