using System;

/// <summary>
/// Универсальный атрибут для связи полей ScriptableVariable с методами.
/// 1. Автоматически отрисовывает приватные поля в инспекторе (через SignalBinderEditor).
/// 2. Подписывает метод на изменение переменной.
/// </summary>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public class BindAttribute : Attribute
{
    public string MethodName { get; }

    /// <summary>
    /// Автоматический поиск метода: On{ИмяПоля}Changed
    /// </summary>
    public BindAttribute()
    {
        MethodName = null;
    }

    /// <summary>
    /// Явное указание имени метода.
    /// </summary>
    public BindAttribute(string methodName)
    {
        MethodName = methodName;
    }
}
