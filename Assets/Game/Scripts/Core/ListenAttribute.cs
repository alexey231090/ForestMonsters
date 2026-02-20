using System;

/// <summary>
/// Атрибут для автоматической подписки методов на GameEvent.
/// Применяется к методу. Укажите имя поля, в котором лежит нужный GameEvent.
/// Пример: [Listen(nameof(GET_onDayStarted))]
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ListenAttribute : Attribute
{
    public string EventFieldName { get; }

    public ListenAttribute(string eventFieldName)
    {
        EventFieldName = eventFieldName;
    }
}
