# 📡 ARCHITECTURE BIBLE: GAME EVENT SYSTEM (SIGNAL-BASED)

Это эволюция событийной системы, которая объединяет классические UnityEvents и чистый C# код через интерфейсы.

---

## 1. Суть системы
Система состоит из трех уровней:
1. **GameEvent (Ассет)**: SO-сигнал в проекте.
2. **SignalBinder (Код)**: Базовый класс для ваших скриптов. Позволяет подписываться на события без лишних компонентов в инспекторе.
3. **ISignalListener**: Интерфейс для тех случаев, когда вы не можете наследоваться от SignalBinder.

---

## 2. Как пользоваться (Workflow)

### Вариант А: Через код (SignalBinder) — Рекомендуемый
Если ваш скрипт — это новый MonoBehaviour, наследуйтесь от `SignalBinder`.

```csharp
public class PlayerAudio : SignalBinder // 1. Наследуемся от биндера
{
    [SerializeField] private GameEvent GET_onJump;

    void Awake()
    {
        // 2. Привязываем метод к сигналу
        Bind(GET_onJump, PlayJumpSound);
    }

    private void PlayJumpSound()
    {
        Debug.Log("Playing Jump Sound!");
    }
}
```

### Вариант Б: Через интерфейс (ISignalListener)
Если наследование занято или нужен специфический контроль.

1. Добавьте интерфейс `ISignalListener`.
2. Реализуйте метод `OnSignalReceived(GameEvent incomingEvent)`.
3. Чтобы система видела этот объект, на нем должен быть либо `SignalBinder`, либо **`GlobalEventRelay`**.

```csharp
public class EnemyAI : MonoBehaviour, ISignalListener
{
    public void OnSignalReceived(GameEvent incomingEvent)
    {
        Debug.Log($"Signal {incomingEvent.name} received!");
    }
}
```

---

## 3. Инструменты отладки (GameEventEditor)

Теперь, если вы выделите файл `GameEvent` в папке `Assets`, инспектор покажет:

1. **⚡ Raise Event**: Кнопка для ручного вызова события (работает в Play Mode).
2. **🎬 Active Listeners (Scene)**: Список всех объектов на текущей сцене, которые слушают этот ивент, с указанием методов, которые они вызывают.
3. **📦 Prefab References (Project)**: Список всех префабов в проекте, которые используют это событие.

---

## 4. Главные правила (Best Practices)

1. **Память (OnEnable/OnDisable)**: `SignalBinder` и `GlobalEventRelay` автоматически подписываются в `OnEnable` и отписываются в `OnDisable`. Если вы переопределяете эти методы в своем классе, **всегда вызывайте `base.OnEnable()` / `base.OnDisable()`**.
3. **Именование переменных в коде**: Чтобы не путаться в инспекторе, используйте префиксы для полей `GameEvent`:
    *   **`call_`** (или `send_`): Для событий, у которых вы вызываете `.Raise()`. Это "выходы" скрипта.
    *   **`GET_`** (или `match_`): Для событий, на которые вы подписываетесь (через `Bind`) или сравниваете в `OnSignalReceived`. Это "входы" скрипта.

---

## 5. Словарь именования ассетов (Project Window)
| Тип | Префикс | Пример |
|---|---|---|
| Game Event | **EV_** | `EV_LevelStarted` |
| Variable (SO) | **VAR_** | `VAR_PlayerHealth` |
| Settings (SO) | **SET_** | `SET_InputConfig` |

---

## 6. Примеры именования в коде (Script Inspector)

```csharp
public class CombatManager : SignalBinder
{
    // ВХОДЫ: Слушаем эти события
    [SerializeField] private GameEvent GET_EnemyDeath;
    [SerializeField] private GameEvent GET_PlayerHit;

    // ВЫХОДЫ: Вызываем эти события
    [SerializeField] private GameEvent call_GameOver;

    void Awake()
    {
        Bind(GET_EnemyDeath, OnEnemyDied);
        Bind(GET_PlayerHit, () => Debug.Log("Player was hit!"));
    }

    private void OnEnemyDied()
    {
        // Логика...
        call_GameOver.Raise(); // Кричим всем, что игра окончена
    }
}
```

