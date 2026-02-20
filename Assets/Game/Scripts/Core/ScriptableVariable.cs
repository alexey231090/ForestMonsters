using UnityEngine;

// Представляем мощный базовый класс для всех переменных в проекте.
// Он объединяет в себе логику сохранения, инициализации и событий.
public abstract class ScriptableVariable<T> : ScriptableVariableBase
{
    [Header("Settings")]
    [Tooltip("Начальное значение при запуске игры.")]
    public T InitialValue;


    [System.NonSerialized]
    private T _value;

    public T Value
    {
        get => _value;
        set
        {
            // Проверка на то, изменилось ли значение реально (чтобы не спамить ивентами)
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, value)) 
                return;

            _value = value;
            Raise();
        }
    }

    protected virtual void OnEnable()
    {
        // Инициализируем значение при запуске
        _value = InitialValue;
    }

    /// <summary>
    /// Устанавливает новое значение и вызывает событие.
    /// </summary>
    public void SetValue(T newValue) => Value = newValue;


}
