using UnityEngine;

/// <summary>
/// Event relay. Subscribes to GameEvent and forwards the signal
/// to all ISignalListener on the same GameObject.
///
/// Used for objects that do NOT inherit from SignalBinder,
/// but implement ISignalListener directly.
///
/// SignalBinders do NOT need this component — they subscribe automatically.
/// </summary>
public class GlobalEventRelay : MonoBehaviour, ISignalListener
{
    [Tooltip("The event that this relay will listen to")]
    [SerializeField] private GameEvent gameEvent;

    public GameEvent GameEventRef => gameEvent;

    private ISignalListener[] _listeners;

    private void OnEnable()
    {
        if (gameEvent == null) return;

        // Find all ISignalListener on this object (except itself)
        _listeners = GetComponents<ISignalListener>();
        gameEvent.RegisterSignal(this);
    }

    private void OnDisable()
    {
        if (gameEvent == null) return;
        gameEvent.UnregisterSignal(this);
    }

    public void OnSignalReceived(GameEvent incomingEvent)
    {
        if (_listeners == null) return;

        foreach (var listener in _listeners)
        {
            // Don't forward to itself to avoid recursion
            if (listener as Object != this)
                listener.OnSignalReceived(incomingEvent);
        }
    }
}
