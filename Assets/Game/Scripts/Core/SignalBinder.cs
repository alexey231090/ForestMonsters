using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Базовый класс-байндер сигналов. Наследуйте от него и привязывайте события к методам через Bind().
/// Автоматически подписывается/отписывается на все забинденные события.
/// 
/// Пример использования:
/// <code>
/// void Awake()
/// {
///     Bind(myEvent, MyMethod);
/// }
/// </code>
/// </summary>
public abstract class SignalBinder : MonoBehaviour, ISignalListener
{
    // Словарь: какой GameEvent за какой метод отвечает
    private readonly Dictionary<GameEvent, Action> _eventMap = new Dictionary<GameEvent, Action>();

    /// <summary>
    /// Привязывает событие к методу. Вызывайте в Awake().
    /// </summary>
    protected void Bind(GameEvent ev, Action action)
    {
        if (ev != null)
            _eventMap[ev] = action;
    }

    /// <summary>
    /// Автоматическая подписка на все забинденные события.
    /// Если переопределяете OnEnable — обязательно вызывайте base.OnEnable().
    /// </summary>
    protected virtual void OnEnable()
    {
        foreach (var ev in _eventMap.Keys)
            ev.RegisterSignal(this);
    }

    /// <summary>
    /// Автоматическая отписка от всех забинденных событий.
    /// Если переопределяете OnDisable — обязательно вызывайте base.OnDisable().
    /// </summary>
    protected virtual void OnDisable()
    {
        foreach (var ev in _eventMap.Keys)
            ev.UnregisterSignal(this);
    }

    /// <summary>
    /// Вызывается когда приходит сигнал. Ищет привязанный метод в словаре и запускает его.
    /// </summary>
    public void OnSignalReceived(GameEvent incomingEvent)
    {
        if (_eventMap.TryGetValue(incomingEvent, out Action action))
            action.Invoke();
    }
}
