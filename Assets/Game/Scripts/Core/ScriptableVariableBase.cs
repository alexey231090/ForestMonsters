using UnityEngine;

/// <summary>
/// Не обобщенный базовый класс для всех ScriptableVariable, 
/// необходим для удобного доступа к полю onValueChanged из систем авто-подписки.
/// </summary>
public abstract class ScriptableVariableBase : ScriptableObject
{
    [Header("Change Event (Optional)")]
    [Tooltip("Сигнал, который будет вызван ПРИ ЛЮБОМ ИЗМЕНЕНИИ этого значения.")]
    public GameEvent onValueChanged;

    /// <summary>
    /// Native C# event for faster and more reliable subscription (doesn't require a GameEvent asset).
    /// </summary>
    public System.Action ValueChanged;
    
    /// <summary>
    /// Принудительно вызывает привязанный GameEvent.
    /// </summary>
    public void Raise()
    {
        if (onValueChanged != null)
            onValueChanged.Raise();
            
        ValueChanged?.Invoke();
    }
}
