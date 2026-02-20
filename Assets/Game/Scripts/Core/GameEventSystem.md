# 📡 ARCHITECTURE BIBLE: GAME EVENT SYSTEM (SIGNAL-BASED)

Система для связи компонентов через ScriptableObject-сигналы.

---

## 1. Варианты использования

### Вариант А: SignalBinder (Рекомендуемый)
Наследование от `SignalBinder`. Работает чисто через код.
*   **Как это работает**: Внутри `SignalBinder` есть словарь (Map). Когда вы вызываете `Bind(event, method)`, метод сохраняется в этот словарь.
*   **Механика подписки**: Подписка и отписка происходят автоматически в `OnEnable` и `OnDisable`.
*   **Плюсы**: Нет лишних UnityEvents в инспекторе, код наглядный, самая высокая производительность.
*   **Важно**: Если вы переопределяете `OnEnable/OnDisable` в своем скрипте, обязательно вызывайте `base.OnEnable() / base.OnDisable()`.

```csharp
// --- ПРИМЕР ОРГАНИЗАЦИИ СКРИПТА ---
public class PlayerEffect : SignalBinder 
{
    [Header("Subscribed Events")] // Кто нам шлет сигналы (Вход)
    [SerializeField] private GameEvent GET_onJump;
    
    [Header("Raising Events")]    // Кому мы шлем сигналы (Выход)
    [SerializeField] private GameEvent CALL_onEffectEnd;

    [Header("Settings Assets")]  // Ссылки на SO-ассеты (Конфиги)
    [SerializeField] private SunSettings SET_SunSettings;

    [Header("Variables SO")] // Ссылки на переменные SO-ассеты для чтения или записи на прямую
    [SerializeField] private VariableSO VAR_VariableSO;

    [Header("Settings")]          // Обычные настройки скрипта (Примитивы)
    [SerializeField] private float effectDuration = 1.0f;
    
    private void OnEnable() 
    {
        // Подписываемся на события
        Bind(GET_onJump, PlayEffect);
    }
    
    // Методы, которые будут вызываться при получении сигнала
    private void PlayEffect() 
    { 
        // Логика...
        

    private void RiseEvent() 
    { 
        // Вызываем событие
        CALL_onEffectEnd.Raise();
    }
}
```

### Вариант Б: GameEventListener
Отдельный компонент, который вешается на GameObject. Используется, когда нельзя изменить базовый класс скрипта.
*   **Как это работает**: Вы добавляете компонент `GameEventListener` на объект, перетаскиваете в него ассет события и в списке `Response()` выбираете нужный метод любого другого скрипта.
*   **Механика подписки**: Работает через классические UnityEvents. Событие вызывает `Response.Invoke()`.
*   **Плюсы**: Гибкость, можно связывать объекты прямо в редакторе без изменения кода.
*   **Минус**: Чуть медленнее из-за работы UnityEvent и больше визуального мусора в инспекторе.

---

## 2. Вариант В: ISignalListener (Кратко)
Интерфейс для прямой реализации метода `OnSignalReceived(GameEvent incomingEvent)`.
*   **Статус**: Не рекомендуется. Требует ручной фильтрации событий через `if/switch`, что усложняет код.

---

## 3. Именование и Организация (Best Practices)
Используйте `[Header]` для визуального разделения потоков данных в инспекторе.

| Тип | Префикс | Группа (Header) | Описание |
|---|---|---|---|
| **Вход** | `GET_` | **Subscribed Events** | Подписка на событие |
| **Выход** | `CALL_` | **Raising Events** | Вызов события через `.Raise()` |
| **Ассет** | `EV_` | (Project View) | Название файла ассета (ScriptableObject) |
| **Ресурсы**| `_` | **Settings Assets** | Ссылки на ScriptableObject конфиги (напр. SunSettings) |
| **Опции** | `_` | **Settings** | Обычные параметры скрипта (float, int, bool) |

---

## 4. Правила для ИИ-агента (AI Instructions)
1. **Лаконичность**: Не пиши длинные пояснения в коде.
2. **Предположение знаний**: Пользователь знает эту архитектуру.
3. **Простота**: Пиши обычные, короткие комментарии.
4. **Наследование**: При редактировании наследников `SignalBinder` всегда следи за вызовом `base` в событиях жизни объекта.
