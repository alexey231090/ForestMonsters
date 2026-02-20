using System;

/// <summary>
/// Атрибут для автоматической подписки методов на события изменения переменных (ScriptableVariable).
/// Добавьте над полем переменной: [OnChanged(nameof(ВашМетод))]
/// Ваш скрипт должен наследоваться от SignalBinder.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class OnChangedAttribute : Attribute
{
    public string MethodName { get; }

    public OnChangedAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
