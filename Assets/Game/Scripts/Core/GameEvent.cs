using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Architecture/Game Event")]
public class GameEvent : ScriptableObject
{
    // ─── Variant B (GameEventListener + UnityEvent) ───
    private readonly List<GameEventListener> eventListeners = new List<GameEventListener>();

    // ─── Variant A (ISignalListener / SignalBinder) ───
    private readonly List<ISignalListener> signalListeners = new List<ISignalListener>();

    // Public access for inspector
    public IReadOnlyList<GameEventListener> EventListeners => eventListeners;
    public IReadOnlyList<ISignalListener> SignalListeners => signalListeners;

    /// <summary>
    /// Raises the event — notifies all subscribers (both variants).
    /// </summary>
    public void Raise()
    {
        // Variant B (Legacy) — backward compatibility
        for (int i = eventListeners.Count - 1; i >= 0; i--)
            eventListeners[i].OnEventRaised();

        // Variant A (Signals)
        for (int i = signalListeners.Count - 1; i >= 0; i--)
            signalListeners[i].OnSignalReceived(this);
    }

    // ─── Variant B Registration ───
    public void RegisterListener(GameEventListener listener) => eventListeners.Add(listener);
    public void UnregisterListener(GameEventListener listener) => eventListeners.Remove(listener);

    // ─── Variant A Registration ───
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
