using UnityEngine;

public class BedTrigger : MonoBehaviour, IInteractable
{
    [SerializeField] private GameEvent CALL_requestTimeSkipEvent;

    public void Interact()
    {
        if (CALL_requestTimeSkipEvent != null)
        {
            CALL_requestTimeSkipEvent.Raise();
            Debug.Log("[BedTrigger] Requesting time skip..");
        }
    }
}
