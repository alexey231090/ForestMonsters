using UnityEngine;

public class EnemyMarker : MonoBehaviour
{
     void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<PlayerInteract>() != null)
        {
            Destroy(gameObject);
        }
    }
    
}