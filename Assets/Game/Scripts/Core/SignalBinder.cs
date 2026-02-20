using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Base class for signal-based components. Inherit from this and bind events using Bind().
/// Automatically subscribes/unsubscribes to all bound events.
/// 
/// Example:
/// <code>
/// protected override void Enable()
/// {
///     Bind(myEvent, MyMethod);
/// }
/// </code>
/// </summary>
public abstract class SignalBinder : MonoBehaviour, ISignalListener
{
    // Dictionary mapping GameEvents to their corresponding actions
    private readonly Dictionary<GameEvent, Action> _eventMap = new Dictionary<GameEvent, Action>();

    /// <summary>
    /// Binds an event to a method. Call this in Awake() or OnEnable().
    /// If called in OnEnable, the subscription happens immediately.
    /// </summary>
    protected void Bind(GameEvent ev, Action action)
    {
        if (ev != null)
        {
            _eventMap[ev] = action;
            // If we bind at runtime (e.g. in OnEnable), register immediately if object is active
            if (isActiveAndEnabled)
            {
                ev.RegisterSignal(this);
            }
        }
    }

    protected void OnEnable()
    {
        foreach (var ev in _eventMap.Keys)
            ev.RegisterSignal(this);
    }

    protected void OnDisable()
    {
        foreach (var ev in _eventMap.Keys)
            ev.UnregisterSignal(this);
    }

    /// <summary>
    /// Called when a signal is received. Looks up the bound method in the dictionary and invokes it.
    /// </summary>
    public void OnSignalReceived(GameEvent incomingEvent)
    {
        if (_eventMap.TryGetValue(incomingEvent, out Action action))
            action.Invoke();
    }
}
