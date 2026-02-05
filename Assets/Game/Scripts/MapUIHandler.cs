using UnityEngine;
using UnityEngine.UIElements;

public class MapUIHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public UIDocument mapUIDoc;
    public MapCameraControl mapCameraControl;

    [Header("UI Customization")]
    [Tooltip("Масштаб кнопок WASD")]
    [Range(0.5f, 3f)]
    public float controlsScale = 1.0f;

    [Tooltip("Отступ снизу (px)")]
    public float bottomOffset = 50f;

    [Tooltip("Отступ справа (px)")]
    public float rightOffset = 50f;

    // Ссылка на контейнер для обновления стилей
    private VisualElement controlsContainer;

    // Флаги нажатия кнопок
    private bool isW, isA, isS, isD;

    // Ссылки на кнопки для подсветки
    private Button btnW, btnA, btnS, btnD;
    private const string CLASS_ACTIVE = "control-btn--active";

    void OnValidate()
    {
        // Позволяет обновлять UI прямо в инспекторе во время игры
        if (Application.isPlaying && mapUIDoc != null && mapUIDoc.rootVisualElement != null)
        {
            ApplyStyles();
        }
    }

    void OnEnable()
    {
        if (mapUIDoc != null)
        {
            var root = mapUIDoc.rootVisualElement;
            if (root != null) BindUI(root);
        }
    }

    public void ShowUI()
    {
        if (mapUIDoc != null)
        {
            mapUIDoc.enabled = true;
            // Перепривязываем при каждом показе, чтобы убедиться, что элементы найдены
            var root = mapUIDoc.rootVisualElement;
            if (root != null) BindUI(root);
        }
    }

    public void HideUI()
    {
        if (mapUIDoc != null) mapUIDoc.enabled = false;
        // Сбрасываем ввод при скрытии
        ResetInput();
    }

    void BindUI(VisualElement root)
    {
        // Находим контейнер кнопок для стилизации
        controlsContainer = root.Q<VisualElement>(className: "controls-container");
        ApplyStyles();

        // Кэшируем кнопки
        btnW = root.Q<Button>("BtnW");
        btnA = root.Q<Button>("BtnA");
        btnS = root.Q<Button>("BtnS");
        btnD = root.Q<Button>("BtnD");

        // Кнопки управления (PointerDown/Up для удержания)
        SetupButton(btnW, (v) => isW = v);
        SetupButton(btnA, (v) => isA = v);
        SetupButton(btnS, (v) => isS = v);
        SetupButton(btnD, (v) => isD = v);

        // Кнопка выхода
        var btnExit = root.Q<Button>("BtnExit");
        if (btnExit != null)
        {
            // Удаляем старые, чтобы не дублировать
            btnExit.clicked -= OnExitClicked;
            btnExit.clicked += OnExitClicked;
        }
    }

    void ApplyStyles()
    {
        if (controlsContainer == null) 
        {
            // Попробуем найти еще раз (если вызов из OnValidate)
            if (mapUIDoc != null && mapUIDoc.rootVisualElement != null)
                controlsContainer = mapUIDoc.rootVisualElement.Q<VisualElement>(className: "controls-container");
        }

        if (controlsContainer != null)
        {
            controlsContainer.style.scale = new Scale(new Vector3(controlsScale, controlsScale, 1f));
            controlsContainer.style.bottom = bottomOffset;
            controlsContainer.style.right = rightOffset;
        }
    }

    // Хелпер для привязки событий нажатия/отпускания
    void SetupButton(Button btn, System.Action<bool> onStateChange)
    {
        if (btn != null)
        {
            btn.RegisterCallback<PointerDownEvent>(evt => onStateChange(true));
            btn.RegisterCallback<PointerUpEvent>(evt => onStateChange(false));
            btn.RegisterCallback<PointerLeaveEvent>(evt => onStateChange(false)); // Если палец ушел с кнопки
        }
    }

    void OnExitClicked()
    {
        if (CctvManager.instance != null)
        {
            CctvManager.instance.ReturnToMenu();
        }
    }

    void ResetInput()
    {
        isW = isA = isS = isD = false;
        if (mapCameraControl != null) mapCameraControl.SetExternalInput(Vector2.zero);
        
        // Сбрасываем визуальную подсветку кнопок
        UpdateButtonHighlight(btnW, false);
        UpdateButtonHighlight(btnA, false);
        UpdateButtonHighlight(btnS, false);
        UpdateButtonHighlight(btnD, false);
    }

    void Update()
    {
        if (mapUIDoc == null || !mapUIDoc.enabled) return;
        if (mapCameraControl == null) return;

        // Проверяем ввод с клавиатуры
        bool keyW = Input.GetKey(KeyCode.W);
        bool keyA = Input.GetKey(KeyCode.A);
        bool keyS = Input.GetKey(KeyCode.S);
        bool keyD = Input.GetKey(KeyCode.D);

        // Обновляем визуальную подсветку (если нажато мышкой ИЛИ клавиатурой)
        UpdateButtonHighlight(btnW, isW || keyW);
        UpdateButtonHighlight(btnA, isA || keyA);
        UpdateButtonHighlight(btnS, isS || keyS);
        UpdateButtonHighlight(btnD, isD || keyD);

        float x = 0f;
        float y = 0f;

        if (isA || keyA) x -= 1f;
        if (isD || keyD) x += 1f;
        if (isS || keyS) y -= 1f;
        if (isW || keyW) y += 1f;

        mapCameraControl.SetExternalInput(new Vector2(x, y));
    }

    private void UpdateButtonHighlight(Button btn, bool active)
    {
        if (btn == null) return;
        if (active) btn.AddToClassList(CLASS_ACTIVE);
        else btn.RemoveFromClassList(CLASS_ACTIVE);
    }
}
