# 📡 ARCHITECTURE BIBLE: GAME EVENT SYSTEM

Система связи компонентов через ScriptableObject-сигналы и динамические переменные.

---

## 🔝 ПРИОРЫТЕТНЫЙ ВАРИАНТ (Через Атрибуты)

Используйте этот метод во всех новых скриптах. Это самый чистый и безопасный способ, который минимизирует количество кода.

### 1. Подписка на Переменные `[OnChanged]`
Автоматически вызывает метод при изменении значения в `ScriptableVariable` (например, Деньги, Здоровье).
```csharp
[SerializeField, OnChanged(nameof(RefreshUI))] 
private FloatVariable VAR_Money;

private void RefreshUI() { /* Обновление текста/графики */ }
```

### 2. Подписка на События `[Listen]`
Автоматически подписывает метод на `GameEvent` ассет.
```csharp
[SerializeField] private GameEvent GET_onDayStarted;

[Listen(nameof(GET_onDayStarted))]
private void StartNewDay() { /* Логика начала дня */ }
```

**⚠️ Важно для ИИ и Людей**: 
- Скрипт **ДОЛЖЕН** наследоваться от `SignalBinder`.
- Регистрация и отписка происходят полностью автоматически через атрибуты.
- Методы, помеченные атрибутами, должны быть без параметров (void).

---

## 🛠 ВАРИАНТ Б (Инспекторный / Legacy)

Если вы **не можете** изменить базовый класс скрипта (он уже наследуется от чего-то другого, не от `SignalBinder`), используйте этот метод.

### GameEventListener (Компонент)
1. Добавьте компонент `GameEventListener` на GameObject.
2. В поле **Event** перетащите ассет события.
3. В списке **Response()** (UnityEvent) добавьте слот и выберите метод вашего скрипта.

---

## 📋 Именование (Best Practices)

| Префикс | Группа (Header) | Описание |
|---|---|---|
| `GET_` | **Subscribed Events** | Поле для ассета GameEvent |
| `VAR_` | **Variables SO** | Ссылка на ScriptableVariable |
| `CALL_` | **Raising Events** | Вызов события через `.Raise()` |

---

## 🤖 Инструкции для ИИ (AI Instructions)

1. **Приоритет Атрибутов**: Всегда используй `[OnChanged]` и `[Listen]` для подписки на события и переменные.
2. **Использование OnEnable**: Если скрипту нужна дополнительная логика при активации (не связанная с подписками), ты МОЖЕШЬ создавать `protected override void OnEnable()`.
3. **Критическое правило**: При переопределении `OnEnable` или `OnDisable` ты **ОБЯЗАН** первым делом вызвать `base.OnEnable()` или `base.OnDisable()`, иначе автоматические подписки не заработают.
4. **Наследование**: Всегда используй `public class Name : SignalBinder`.
