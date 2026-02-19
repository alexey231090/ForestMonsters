using UnityEngine;

/// <summary>
/// Ретранслятор событий. Подписывается на GameEvent и пробрасывает сигнал
/// всем ISignalListener на этом же GameObject.
/// 
/// Используется для объектов, которые НЕ наследуют SmartListener,
/// но реализуют ISignalListener напрямую.
/// 
/// SmartListener'ам этот компонент НЕ нужен — они подписываются автоматически.
/// </summary>
public class GlobalEventRelay : MonoBehaviour, ISignalListener
{
    [Tooltip("Событие, которое этот ретранслятор будет слушать")]
    [SerializeField] private GameEvent gameEvent;

    public GameEvent GameEventRef => gameEvent;

    private ISignalListener[] _listeners;

    private void OnEnable()
    {
        if (gameEvent == null) return;

        // Находим все ISignalListener на этом объекте (кроме самого себя)
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
            // Не пробрасываем самому себе, чтобы не было рекурсии
            if (listener as Object != this)
                listener.OnSignalReceived(incomingEvent);
        }
    }
}
