using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;

/// <summary>
/// Base class for signal-based components. Inherit from this and bind events using Bind().
/// Automatically subscribes/unsubscribes to all bound events.
/// Also supports Auto-Binding using the [OnChanged] attribute on ScriptableVariable fields.
/// </summary>
public abstract class SignalBinder : MonoBehaviour, ISignalListener
{
    // Dictionary mapping GameEvents to their corresponding actions
    private readonly Dictionary<GameEvent, Action> _eventMap = new Dictionary<GameEvent, Action>();

    // Caches for reflection-based Auto-Binding to avoid lookup overhead
    private static readonly Dictionary<Type, List<(FieldInfo field, string methodName)>> _autoBindVariablesCache = new Dictionary<Type, List<(FieldInfo field, string methodName)>>();
    private static readonly Dictionary<Type, List<(FieldInfo eventField, MethodInfo method)>> _autoBindEventsCache = new Dictionary<Type, List<(FieldInfo eventField, MethodInfo method)>>();
    
    private bool _autoBindingsInitialized = false;

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

    protected virtual void OnEnable()
    {
        InitializeAutoBindings();

        foreach (var ev in _eventMap.Keys)
            ev.RegisterSignal(this);
    }

    protected virtual void OnDisable()
    {
        foreach (var ev in _eventMap.Keys)
            ev.UnregisterSignal(this);
    }

    /// <summary>
    /// Processes [OnChanged] and [Listen] attributes and automatically binds methods.
    /// </summary>
    private void InitializeAutoBindings()
    {
        if (_autoBindingsInitialized) return;
        _autoBindingsInitialized = true;

        Type type = this.GetType();

        // 1. Populate caches for this Type if it's the first time
        if (!_autoBindVariablesCache.ContainsKey(type))
        {
            var varBindings = new List<(FieldInfo, string)>();
            var eventBindings = new List<(FieldInfo, MethodInfo)>();
            
            // A. Find fields with [OnChanged]
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<OnChangedAttribute>();
                if (attr != null && typeof(ScriptableVariableBase).IsAssignableFrom(field.FieldType))
                {
                    varBindings.Add((field, attr.MethodName));
                }
            }
            
            // B. Find methods with [Listen]
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes<ListenAttribute>();
                foreach (var attr in attributes)
                {
                    // Find the field that matches the name provided in the attribute
                    FieldInfo eventField = type.GetField(attr.EventFieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    
                    if (eventField != null && typeof(GameEvent).IsAssignableFrom(eventField.FieldType))
                    {
                        eventBindings.Add((eventField, method));
                    }
                    else
                    {
                        Debug.LogError($"[SignalBinder] Auto-Binding [Listen] failed on {type.Name}: Could not find a GameEvent field named '{attr.EventFieldName}'!");
                    }
                }
            }

            _autoBindVariablesCache[type] = varBindings;
            _autoBindEventsCache[type] = eventBindings;
        }

        // 2. Apply [OnChanged] bindings from cache to this instance
        foreach (var binding in _autoBindVariablesCache[type])
        {
            var variable = binding.field.GetValue(this) as ScriptableVariableBase;
            if (variable != null && variable.onValueChanged != null)
            {
                MethodInfo method = type.GetMethod(binding.methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (method != null)
                {
                    Action action = (Action)Delegate.CreateDelegate(typeof(Action), this, method);
                    Bind(variable.onValueChanged, action);
                }
                else
                {
                    Debug.LogError($"[SignalBinder] Auto-Binding failed on {gameObject.name}: Method '{binding.methodName}' not found in {type.Name}!");
                }
            }
        }
        
        // 3. Apply [Listen] bindings from cache to this instance
        foreach (var binding in _autoBindEventsCache[type])
        {
            var gameEvent = binding.eventField.GetValue(this) as GameEvent;
            if (gameEvent != null)
            {
                Action action = (Action)Delegate.CreateDelegate(typeof(Action), this, binding.method);
                Bind(gameEvent, action);
            }
            // It's okay if gameEvent is null, it just means it wasn't assigned in inspector
        }
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
