# 📚 Документация для AI: ForestMonsters Game Scripts

> **Назначение:** Этот документ создан для быстрой ориентации AI-ассистента в кодовой базе проекта ForestMonsters. Содержит описание архитектуры, основных систем, классов и их взаимодействий.
>
> **Статус:** ✅ АКТУАЛЬНО - Документ обновляется и поддерживается в актуальном состоянии

---

## 📋 Обзор проекта

**Тип игры:** Unity-проект в жанре tycoon/management с элементами выживания

**Основная механика:** Игрок ловит врагов в ловушки ночью, размещает их на платформах днем, зарабатывает деньги от посетителей парка.

**Архитектурный стиль:** Модульная система с использованием интерфейсов и событийной архитектуры (GameEvent System)

---

## 🏗️ Архитектура и основные системы

### 📡 GameEvent System (Приоритетная система связи)

**Файл документации:** `Assets/Game/Scripts/Core/GameEventSystem.md`

Система связи компонентов через ScriptableObject-сигналы и динамические переменные.

#### **Ключевые компоненты:**

| Компонент | Назначение |
|-----------|-----------|
| `SignalBinder` | Базовый класс для автоматической подписки на события через атрибуты |
| `[Bind]` | Атрибут для автоматической подписки на изменения SO-переменных |
| `[SerializeField, Bind]` | Комбинация для авто-отрисовки в инспекторе и авто-подписки |
| `GameEvent` | ScriptableObject-событие для вызова из любого места |
| `GameEventListener` | Компонент для прослушивания событий (если нельзя наследовать SignalBinder) |
| `ScriptableVariable` | Базовый класс для SO-переменных (IntVariable, FloatVariable, BoolVariable) |

#### **Магический вариант использования:**

```csharp
// Автоматическая подписка на изменение переменной
[SerializeField, Bind] IntVariable VAR_TrapsCount;

// Метод вызывается автоматически при изменении значения
// Имя метода: On{ИмяПоля}Changed()
private void OnVAR_TrapsCountChanged() {
    UpdateUI(); // Логика при изменении количества ловушек
}

// Для событий (GameEvent)
// Имя метода: On{ИмяПоля}()
[SerializeField, Bind] GameEvent EV_OnDeath;

// Метод вызывается автоматически при вызове события
private void OnEV_OnDeath() {
    // Логика при смерти
}
```

#### **Своё имя метода (кастомизация):**

Если вы хотите использовать своё название метода вместо стандартного `On{ИмяПоля}Changed()`, передайте имя метода в атрибут `[Bind]`:

```csharp
// Для переменных - указываем имя метода без параметров
[SerializeField, Bind("OnTrapCountChanged")] IntVariable VAR_TrapsCount;

private void OnTrapCountChanged() {
    UpdateUI(); // Кастомное имя метода
}

// Для событий - аналогично
[SerializeField, Bind("HandleDeath")] GameEvent EV_OnDeath;

private void HandleDeath() {
    // Кастомное имя метода для события
}
```

**Важно:**
- Метод должен быть `void` и **без параметров**
- Метод может быть `private`, `protected` или `public`
- `[SerializeField]` обязателен для сохранения ссылки в инспекторе

#### **Именование полей в скриптах:**

| Префикс | Группа | Описание |
|---------|--------|----------|
| `GET_` | **Subscribed Events** | Поле для чтения ассета GameEvent |
| `VAR_` | **Variables SO** | Ссылка на ScriptableVariable |
| `CALL_` | **Raising Events** | Поле для вызова события через `.Raise()` |

#### **Именование SO-ассетов:**

| Префикс | Группа | Описание |
|---------|--------|----------|
| `EV_` | **Game Events** | События (EV_OnTrapPickedUp, EV_OnEnemyCaught) |
| `VAR_` | **Variables SO** | Переменные (VAR_TrapsCount, VAR_Health) |
| `SET_` | **Settings SO** | Настройки (SET_GameSettings) |

---

### 🔌 Интерфейсная архитектура (Модульность)

**Принцип:** Все взаимодействия между системами происходят через интерфейсы, что обеспечивает слабую связанность и возможность легкой замены реализаций.

#### **IInteractableTrap** - Интерфейс для интерактивных ловушек

**Файл:** `Assets/Game/Scripts/Core/IInteractableTrap.cs`

```csharp
public interface IInteractableTrap
{
    bool CanBePickedUp { get; }      // Можно ли поднять ловушку
    void OnPickUp(Transform hand);   // Вызывается при поднятии
    void OnDrop();                   // Вызывается при отпускании
    bool HasCatch();                 // Есть ли пойманный враг
}
```

**Преимущества:**
- `PlayerInteract` и `PlayerCarrier` не зависят от конкретной реализации `Trap2`
- Любая ловушка, реализующая интерфейс, будет работать с системой игрока
- Легко добавлять новые типы ловушек без изменения кода игрока

---

## 📦 Описание основных скриптов

### 1. **EnemyAi.cs** - AI врагов

**Назначение:** Управление поведением врагов (патрулирование, преследование игрока, реакция на ловушки).

**Основные состояния:**
1. **Патрулирование** (`enablePatrol`) - случайное движение в радиусе от начальной позиции
2. **Преследование** (`isChasing`) - активная погоня за целью (игроком)
3. **Оглушение** (`trapStunned`) - временная остановка после активации ловушки

**Ключевые параметры:**
- `activationRadius` - радиус обнаружения цели (начинает преследование)
- `disengageDistance` - дистанция отключения преследования
- `patrolRadius` - радиус патрулирования от начальной точки
- `patrolWaitTime` - время ожидания на точке патрулирования

**Важные методы:**
- `MoveToTarget()` - логика преследования через NavMeshAgent
- `UpdatePatrol()` - обновление патрулирования
- `StartPatrol()`, `StopPatrol()` - управление патрулированием
- `StunByTrap(float duration)` - оглушение от ловушки

**Интеграция с PlayMaker:**
- `isPatrolMode` - флаг для PlayMaker FSM
- `PatrolWithDetection()` - универсальный метод для PlayMaker
- `StartPatrolWithDetection()` - запуск патрулирования с обнаружением

**Технические детали:**
- Использует `UnityEngine.AI.NavMeshAgent` для навигации
- Применяет гистерезис для предотвращения частых переключений режимов

---

### 2. **PlayerInteract.cs** - Система взаимодействий игрока

**Назначение:** Обработка всех действий игрока (строительство, взаимодействие с объектами).

**Наследование:** `SignalBinder` (для работы с `[Bind]` атрибутами)

**Основные режимы:**

#### **Режим строительства:**
- **Активация:** Клавиши `1` (ловушка) или `2` (камера)
- **Механика "призраков":**
  - При выборе предмета создается полупрозрачная копия (ghost) на месте установки
  - Призрак показывается только при взгляде на землю (`groundLayer`)
  - **Таймер автоотключения:** Если игрок не смотрит на землю 5 секунд (`ghostTimeout`) - режим строительства отключается
  - **Фитиль в UI:** Визуальный индикатор времени до автоотключения

- **Установка:** ЛКМ с удержанием (`placeHoldTimeRequired`) устанавливает предмет. Камеры (2) ставятся только на `treeLayer`, ловушки (1) — только на `groundLayer`.

#### **Режим переноски:**
- Если `PlayerCarrier.IsCarrying() == true` - режим строительства отключается

#### **Взаимодействие (E):**
- **Поднятие объектов:** Удержание E на ловушке/камере (через `PlayerCarrier`)
- **Монитор:** Открытие режима монитора (`CctvManager`)
- **Кровать:** Пропуск текущей фазы через `BedTrigger` → `SunMovementController.TogglePhase()`
- **Платформа:** Размещение существа на платформе (`ParkPlatform.TryPlaceMonster()`)

**Layer-маски:**
```csharp
public LayerMask interactLayer; // Слой предметов (ловушки, мониторы)
public LayerMask groundLayer;   // Слой для строительства ловушек
public LayerMask treeLayer;     // Слой для установки камер
```

**SO-переменные с [Bind]:**
```csharp
[SerializeField, Bind] IntVariable VAR_SelectedSlot;
[SerializeField, Bind] FloatVariable VAR_BuildFuseProgress;
[SerializeField, Bind] BoolVariable VAR_IsBuildFuseActive;
[SerializeField, Bind] FloatVariable VAR_PickupProgress;
```

**Auto-reaction методы (вызываются автоматически):**
```csharp
private void OnVAR_SelectedSlotChanged() {
    // Реакция на смену предмета
    ghostTimer = ghostTimeout;
    DestroyGhost();
}

private void OnVAR_BuildFuseProgressChanged() {
    // UI обновляется автоматически через SO
}

private void OnVAR_IsBuildFuseActiveChanged() {
    // Реакция на изменение режима стройки
}

private void OnVAR_PickupProgressChanged() {
    // UI обновляется автоматически через SO
}
```

**Ключевые методы:**
- `UpdateGhostLogic()` - управление призраками и таймером (учитывает слои установки для каждого предмета)
- `TryPlaceItem()` - установка предмета на землю (ловушки) или деревья (камеры)
- `HandleInteraction()` - обработка взаимодействий через E (использует `IInteractableTrap`)
- `ChangeItem()`, `DisableBuildMode()` - управление режимами

---

### 3. **PlayerCarrier.cs** - Система переноски объектов

**Назначение:** Физическая переноска ловушек с пойманными врагами.

**Наследование:** `SignalBinder`

**Логика работы:**

1. **Поднятие:** Удержание E на объекте → `ProcessHold()` → `PerformPickup()`
   - Если ловушка пустая → возврат в инвентарь (`trapsCount++`)
   - Если ловушка с добычей → физическая переноска (`PickUpPhysical()`)
   - При переноске объект прикрепляется к `holdPoint` через DOTween

2. **Переноска:**
   - Объект следует за игроком через `holdPoint`
   - Коллайдеры отключаются для предотвращения столкновений

3. **Сброс:** Нажатие E при переноске → `TryDrop()` → `DropPhysical()`
   - Луч вниз от `holdPoint` ищет землю
   - Объект анимированно падает с утоплением (`dropEmbedDepth`)

**Использование интерфейса:**
```csharp
private IInteractableTrap carriedTrap; // Поле использует интерфейс

void PickUpPhysical(IInteractableTrap trap) {
    carriedTrap = trap;
    trap.OnPickUp(holdPoint); // Вызов метода интерфейса
}

void DropPhysical(Vector3 floorPos) {
    IInteractableTrap trapToDrop = carriedTrap;
    trapToDrop.OnDrop(); // Вызов метода интерфейса
}
```

**Зависимости:**
- Использует `DOTween` для анимаций
- Работает совместно с `PlayerInteract`

---

### 4. **Trap2.cs** - Ловушка (модульная реализация)

**Назначение:** Логика захвата врагов и обработка состояния ловушки.

**Реализация интерфейса:** `IInteractableTrap`

**SO Настройки:**
- `TrapSettings settings` - ScriptableObject ассет с настройками ловушки
- **Файл настроек:** `Assets/Game/Scripts/Data/SO/SET_TrapSettings.asset`

**Параметры TrapSettings:**
```csharp
[Header("Настройки Сферы Обнаружения")]
public float detectionRadius = 1.0f;      // Радиус обнаружения
public Vector3 sphereOffset = Vector3.up * 0.5f;  // Смещение сферы
public LayerMask detectionLayer;           // Слои для обнаружения
public float checkInterval = 0.1f;         // Интервал проверки (сек)

[Header("Настройки Захвата")]
public float attractionSpeed = 0.5f;       // Скорость притягивания врага

[Header("Настройки Переноски")]
public float pickUpDuration = 0.3f;        // Время анимации поднятия
public float dropDuration = 0.5f;          // Время анимации установки

[Header("Визуал")]
public Color gizmoColorSearching = Color(0, 1, 0, 0.3);  // Цвет когда ищет
public Color gizmoColorCaught = Color(1, 0, 0, 0.3);     // Цвет когда поймал
```

**Работа ловушки:**

1. **Обнаружение через сферу:**
   - `Physics.OverlapSphere()` в радиусе `settings.detectionRadius`
   - Проверка с интервалом `settings.checkInterval` (оптимизация)
   - Слои обнаружения через `settings.detectionLayer`

2. **Захват врага:**
   - Проверка тега "Enemy" и флага `!enemyAI.IsCaught`
   - Отключает `EnemyAi` и `NavMeshAgent` врага
   - Притягивает врага к `capturePoint` через DOTween со скоростью `settings.attractionSpeed`
   - Включает анимацию (`animatorCell`) и частицы (`captureParticles`)
   - Включает физический коллайдер

3. **Доставка в парк:**
   - Проверка тега "ParkTrigger"
   - Вызов `ParkManager.TryDeliverMonster()`
   - Возврат ловушки в инвентарь (`VAR_TrapsCount.ApplyChange(1)`)
   - Удаление ловушки

**Реализация IInteractableTrap:**
```csharp
public bool CanBePickedUp => isActive && !isDelivered;

void IInteractableTrap.OnPickUp(Transform hand) {
    isActive = false; // Отключаем проверку сферы
    transform.SetParent(hand);
    transform.DOLocalMove(Vector3.zero, settings.pickUpDuration);
    transform.DOLocalRotate(Vector3.zero, settings.pickUpDuration);
}

void IInteractableTrap.OnDrop() {
    transform.SetParent(null);
    isActive = true; // Включаем проверку сферы
}

public bool HasCatch() => isUsed && caughtEnemy != null;
```

**Состояния:**
- `isActive` - активна ли проверка сферы
- `isUsed` - пойман ли враг
- `isDelivered` - доставлена ли в парк

**Визуализация:**
- `OnDrawGizmos()` рисует сферу обнаружения с цветами из `settings.gizmoColorSearching` / `settings.gizmoColorCaught`

---

### 5. **CctvManager.cs** - Менеджер камер наблюдения

**Назначение:** Управление системой мониторинга (камеры безопасности, карта).

**Режимы работы:**

1. **Режим монитора** (`isMonitorActive`):
   - Отключает управление игроком (`playerController.enabled = false`)
   - Показывает UI через `MonitorUIHandler`
   - Скрывает HUD игрока через `playerHUD.Hide()`
   - Управление: E/Escape - выход

2. **Просмотр камер** (`isWatchingCameras`):
   - Активирует одну из `securityCameras`
   - Переключение: D - следующая, A - предыдущая
   - Возврат в меню: E/Escape

3. **Просмотр карты** (`isWatchingMap`):
   - Активирует `mapCamera`
   - Возврат в меню: E/Escape

**Регистрация камер:**
- Камеры регистрируются через `RegisterCamera(Camera)`
- Автоматически отключаются при регистрации

**Защита от багов:**
- Таймеры `lastExitTime`, `lastEnterTime` предотвращают двойные срабатывания

---

### 6. **PlayerUIHandler.cs** - UI игрока

**Назначение:** Управление UI через UI Toolkit (инвентарь, индикаторы).

**Основные элементы:**
- `slotTrap`, `slotCamera` - слоты инвентаря
- `fuseTrap`, `fuseCam` - визуализация фитиля (таймер автоотключения режима строительства)
- Фитиль состоит из 4 частей: `Top`, `Right`, `Bottom`, `Left` (анимируются по периметру)

**Методы:**
- `Show()` / `Hide()` - правильное включение/выключение HUD с перепривязкой элементов
- `BindUI()` - выполняется при каждом включении для обновления ссылок `VisualElement` (защита от багов UI Toolkit)
- `SelectSlot(int)` - выбор слота (0=ловушка, 1=камера, -1=ничего)
- `SetFuseActive(int, bool)` - показ/скрытие фитиля
- `SetFuseProgress(int, float)` - установка прогресса фитиля (0..1)

---

### 7. **MonitorUIHandler.cs** - UI монитора

**Назначение:** Управление UI режима монитора (покупки, просмотр камер/карты).

**Функционал:**
- Отображение денег и инвентаря
- Кнопки покупки (`BuyTrap`, `BuyCamera`)
- Переключение в режим камер/карты
- Выход из режима монитора

**Привязка к UI:**
- Использует UI Toolkit с элементами: `MoneyText`, `InfoText`, `BtnCameras`, `BtnMap`, `BtnExit`, `BtnBuyTrap`, `BtnBuyCam`

---

### 8. **EnemySpawner.cs** - Спавнер врагов

**Назначение:** Создание врагов ночью в фиксированных или новых случайных точках.

**Логика спавна:**

1. **Фиксированные точки:** После первой ночи выбранные точки спавна закрепляются за врагами.

2. **Спавн по окружности:** Враги спавнятся строго на окружности радиуса `spawnRadius`, что оставляет центр точки спавна пустым и предотвращает "кучкование".

3. **Обработка пойманных:**
   - При наступлении дня (`ClearEnemies`) пойманные враги (`IsCaught == true`) **НЕ удаляются**
   - Точки спавна, на которых были пойманы враги, считаются "вакантными" и перераспределяются на следующую ночь
   - Непойманные враги удаляются утром, но за ними сохраняется их старая точка спавна

**Методы:**
- `SpawnEnemies()` - добирает недостающие точки до `enemiesPerNight` и создает монстров
- `ClearEnemies()` - удаляет только свободных монстров, освобождая точки пойманных

---

### 9. **VisitorSpawner.cs** - Спавнер посетителей (виртуальный)

**Назначение:** Симуляция посетителей парка днем (не физический спавн, а виртуальная логика).

**Механика:**
- `StartNewDay()` - запускает корутину `VirtualVisitorRoutine()`
- Посетители "приходят" виртуально (через корутину)
- Каждый посетитель платит: `количество_платформ * pricePerMeme`
- Если платформ нет (`activePlatforms.Count == 0`) - посетитель не платит

**Настройки:**
- `minVisitors`, `maxVisitors` - диапазон количества посетителей за день
- Задержка между посетителями: 2-5 секунд

---

### 10. **ParkPlatform.cs** - Платформа для размещения существ

**Назначение:** Место размещения пойманных существ для заработка.

**Механика:**
- `TryPlaceMonster()` - вызывается при взаимодействии с E
- `PlaceMonsterDirectly()` - прямая установка монстра (используется `ParkManager` при доставке в клетке)

**Регистрация:**
- Платформа регистрирует себя в `ParkManager.activePlatforms` при активации, что увеличивает доход от посетителей.

---

### 11. **ParkManager.cs** - Менеджер парка

**Назначение:** Автоматизация приема пойманных существ.

**Логика:**
- Хранит список всех `ParkPlatform`
- `TryDeliverMonster()` - ищет первую свободную платформу и размещает на ней монстра
- Вызывается из `Trap2.cs` при касании триггера с тегом `ParkTrigger`

---

### 12. **SunMovementController.cs** - Контроллер движения солнца

**Назначение:** Управление вращением солнца и визуальными эффектами (освещение, туман) на основе прогресса фазы.

**Ключевые компоненты:**

- **Параметры вращения:**
  - `sunDirectionY` - поворот солнца по горизонту (ось Y)
  - `sunTrajectoryTilt` - наклон траектории солнца (ось Z)

- **Визуальные параметры:**
  - `sunLight` - ссылка на компонент света солнца
  - `dayFog`, `nightFog` - цвета тумана для дня и ночи
  - `dayIntensity`, `nightIntensity` - интенсивность освещения

**Методы управления:**
- `UpdateSunPosition(float progress, bool isNight)` - обновление позиции солнца на основе прогресса фазы (0.0-1.0)
- `SetVisualsForDay()` / `SetVisualsForNight()` - установка визуальных параметров

**Интеграция:**
- Самодостаточный класс через SignalBinder
- Автоматически вызывает GameEvent при смене дня/ночи
- Связан с системой освещения и туманом Unity

---

### 12.1 **BedTrigger.cs** - Триггер кровати

**Назначение:** Взаимодействие с кроватью для пропуска времени (день ↔ ночь).

**Реализация интерфейса:** `IInteractable`

**Логика работы:**
- Игрок нажимает E на кровати
- Вызывает `Interact()` → `CALL_requestTimeSkipEvent.Raise()`
- `SunMovementController` слушает событие и переключает фазу

**Интеграция:**
- Связан с `SunMovementController` через GameEvent `CALL_requestTimeSkipEvent`

---

### 13. **MapUIHandler.cs** - Обработчик UI для режима карты

**Назначение:** Управление интерфейсом карты с кнопками WASD для перемещения камеры.

**Ключевые компоненты:**

- **Зависимости:**
  - `mapUIDoc` - ссылка на документ UI Toolkit
  - `mapCameraControl` - ссылка на контроллер камеры карты

- **Настройки (UI Customization):**
  - `controlsScale` - масштаб кнопок WASD
  - `bottomOffset`, `rightOffset` - положение контейнера кнопок

**Методы управления:**
- `ShowUI()` / `HideUI()` - отображение/скрытие интерфейса
- `BindUI(VisualElement root)` - привязка элементов и кэширование кнопок
- `UpdateButtonHighlight(Button, bool)` - подсветка кнопок через класс `.control-btn--active`

**Интеграция:**
- Использует UI Toolkit для построения интерфейса
- Взаимодействует с `CctvManager` для выхода из режима карты
- Связан с `MapCameraControl` для передачи ввода от кнопок

---

### 14. **MapCameraControl.cs** - Контроллер камеры для режима карты

**Назначение:** Управление ортографической камерой в режиме карты с возможностью перемещения и масштабирования.

**Ключевые компоненты:**

- **Параметры управления:**
  - `panSpeed` - скорость перемещения камеры
  - `zoomSpeed` - скорость масштабирования
  - `minZoom`, `maxZoom` - минимальное и максимальное значения масштаба

- **Визуал (Map Visuals):**
  - `OnPreCull()` - временно отключает тени, туман и выставляет `ambientLight`
  - `OnPostRender()` - восстанавливает настройки графики сцены

**Методы управления:**
- `SetExternalInput(Vector2 input)` - установка ввода от UI кнопок (WASD)
- `Start()` - инициализация начальной позиции камеры

**Интеграция:**
- Поддерживает управление мышью (перетаскивание), клавиатурой (WASD/стрелки) и UI кнопками
- Использует колесико мыши для масштабирования
- Ограничивает перемещение камеры в заданных границах

---

## 🔄 Потоки данных и взаимодействия

### **Цикл день/ночь:**
```
SunMovementController.Update()
  → HandleTimeCycle()
    → Расчет прогресса фазы
    → UpdateSunPosition(progress, isNight)
    → Проверка окончания фазы → TogglePhase()
  → StartDay() / StartNight()
    → isNight = false/true
    → Вызывает GameEvent: call_onDayStarted / call_onNightStarted
    → VisitorSpawner.StartNewDay() / StopSpawning() (через [Listen])
    → EnemySpawner.ClearEnemies() / SpawnEnemies() (через [Bind])
```

### **Процесс ловли врага (модульный):**
```
EnemyAi (патрулирование/преследование)
  → Вход в сферу обнаружения Trap2
  → Trap2.CheckOverlap() находит врага
  → Trap2.TryCatchEnemy()
    → enemyAI.IsCaught = true
    → enemyAI.enabled = false
    → DOMove к capturePoint
    → physicalCollider.enabled = true
```

### **Поднятие ловушки игроком (через интерфейс):**
```
PlayerInteract.HandleInteraction()
  → Raycast на interactLayer
  → IInteractableTrap trap = GetComponent<IInteractableTrap>()
  → if (trap != null && trap.CanBePickedUp)
    → PlayerCarrier.ProcessHold()
      → PlayerCarrier.PerformPickup()
        → if (trap.HasCatch()) → PickUpPhysical(trap)
          → trap.OnPickUp(holdPoint)
```

### **Сброс ловушки (через интерфейс):**
```
Input E → PlayerCarrier.TryDrop()
  → Raycast вниз от holdPoint
  → DropPhysical()
    → trapTransform.SetParent(null)
    → DOMove с Ease.OutBounce
    → trap.OnDrop() (восстанавливает isActive)
```

### **Система строительства (с [Bind]):**
```
Input 1/2 → VAR_SelectedSlot.Value = index
  → [Bind] вызывает OnVAR_SelectedSlotChanged()
    → ghostTimer = ghostTimeout
    → DestroyGhost()
    → VAR_IsBuildFuseActive.Value = true
    → VAR_BuildFuseProgress.Value = 1f
  → UpdateGhostLogic() создает/обновляет призрака
  → ЛКМ (удержание) → HandlePlacementHold()
    → placeHoldTimer += Time.deltaTime
    → VAR_PickupProgress.Value = progress
    → placeHoldTimer >= placeHoldTimeRequired → TryPlaceItem()
```

---

## 🔑 Важные паттерны и соглашения

### **Singleton-паттерн:**
- `CctvManager.instance`
- `ParkManager.instance`

### **Использование тегов:**
- `"Enemy"` - враги
- `"Player"` - игрок
- `"ParkTrigger"` - триггер доставки в парк

### **Layer-маски:**
- `interactLayer` - объекты взаимодействия (ловушки, камеры)
- `groundLayer` - поверхность для строительства ловушек
- `treeLayer` - поверхность для установки камер
- `detectionLayer` - слои для обнаружения ловушкой

### **Индексы предметов:**
- `0` - ловушка
- `1` - камера
- `-1` - ничего не выбрано

### **Зависимости:**
- `DOTween` - для анимаций (PlayerCarrier, Trap2)
- `UnityEngine.AI` - для навигации врагов
- `UI Toolkit` - для UI (PlayerUIHandler, MonitorUIHandler, MapUIHandler)

---

## ⚠️ Важные замечания для AI

### 1. **Модульность через интерфейсы:**
   - Всегда используйте `IInteractableTrap` вместо прямого обращения к `Trap2`
   - Это позволяет добавлять новые типы ловушек без изменения кода игрока

### 2. **GameEvent System (приоритет):**
   - Для связи между системами используйте `[Bind]` атрибуты
   - Наследуйте скрипты от `SignalBinder` для работы авто-подписки
   - Всегда вызывайте `base.OnEnable()` при переопределении `OnEnable()`

### 3. **UI Toolkit жизненный цикл:**
   - При выключении `UIDocument.enabled = false` элементы теряют актуальность
   - Всегда используйте `Show()` / `BindUI()` после повторного включения

### 4. **Особенности строительства:**
   - Режим строительства автоматически отключается при переноске
   - Таймер автоотключения сбрасывается при взгляде на целевой слой (`groundLayer` для ловушек, `treeLayer` для камер)
   - ЛКМ с удержанием требуется для установки (`placeHoldTimeRequired`)
   - Камеры ставятся **только** на объекты со слоем `tree`

### 5. **Система спавна монстров:**
   - Спавн происходит при `StartNight()` и при смене фазы через кровать
   - Пойманные враги сохраняют свои точки спавна

### 6. **Рендеринг карты:**
   - `MapCameraControl.OnPreCull()` меняет глобальные настройки на один кадр
   - Это создает эффект "светлой карты" без теней

### 7. **Флаг IsCaught:**
   - Враги с `IsCaught = true` не могут быть пойманы другой ловушкой
   - Проверяйте этот флаг перед захватом

---

## 🔍 Поиск по функциональности

| Задача | Скрипт/Метод |
|--------|-------------|
| Управление день/ночь | `SunMovementController.TogglePhase()`, `StartDay()`, `StartNight()` |
| Спавн врагов | `EnemySpawner.SpawnEnemies()` |
| Доставка в парк | `ParkManager.TryDeliverMonster()` |
| Логика патрулирования | `EnemyAi.UpdatePatrol()` |
| Установка предметов | `PlayerInteract.TryPlaceItem()` |
| Переноска объектов | `PlayerCarrier.PerformPickup()`, `trap.OnPickUp()` |
| Захват врага | `Trap2.TryCatchEnemy()` |
| UI инвентаря | `PlayerUIHandler` |
| Режим монитора | `CctvManager.EnterMonitorMode()` |
| Размещение на платформе | `ParkPlatform.TryPlaceMonster()` |
| Авто-реакция на SO | `[SerializeField, Bind]` + `On{ИмяПоля}Changed()` |
| Модульная ловушка | `IInteractableTrap` интерфейс |

---

## 📖 Глоссарий терминов

| Термин | Значение |
|--------|----------|
| **Ghost** | Полупрозрачный префаб для предпросмотра места установки |
| **Fuse** | Визуальный индикатор таймера автоотключения режима строительства |
| **SignalBinder** | Базовый класс для автоматической подписки на события |
| **SO Variable** | ScriptableObject-переменная для обмена данными между системами |
| **IInteractableTrap** | Интерфейс для всех интерактивных ловушек |
| **OverlapSphere** | Метод обнаружения врагов в радиусе ловушки |

---

**Версия документа:** 3.1 (Ограничена установка камер слоем tree)
**Последнее обновление:** 26.02.2026
**Автор:** AI Assistant для проекта ForestMonsters
**Статус:** ✅ Поддерживается и обновляется
