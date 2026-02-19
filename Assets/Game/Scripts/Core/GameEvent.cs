using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Game Event")]
public class GameEvent : ScriptableObject
{
    // ─── Старая система (GameEventListener + UnityEvent) ───
    private readonly List<GameEventListener> eventListeners = new List<GameEventListener>();

    // ─── Новая система (ISignalListener / SmartListener) ───
    private readonly List<ISignalListener> signalListeners = new List<ISignalListener>();

    // Публичный доступ для инспектора
    public IReadOnlyList<GameEventListener> EventListeners => eventListeners;
    public IReadOnlyList<ISignalListener> SignalListeners => signalListeners;

    /// <summary>
    /// Вызывает событие — оповещает всех подписчиков (обеих систем).
    /// </summary>
    public void Raise()
    {
        // Старая система — обратная совместимость
        for (int i = eventListeners.Count - 1; i >= 0; i--)
            eventListeners[i].OnEventRaised();

        // Новая система — сигналы
        for (int i = signalListeners.Count - 1; i >= 0; i--)
            signalListeners[i].OnSignalReceived(this);
    }

    // ─── Регистрация старой системы ───
    public void RegisterListener(GameEventListener listener) => eventListeners.Add(listener);
    public void UnregisterListener(GameEventListener listener) => eventListeners.Remove(listener);

    // ─── Регистрация новой системы ───
    public void RegisterSignal(ISignalListener listener)
    {
        if (!signalListeners.Contains(listener))
            signalListeners.Add(listener);
    }

    public void UnregisterSignal(ISignalListener listener)
    {
        signalListeners.Remove(listener);
    }
}
