using UnityEngine;
using UnityEngine.UIElements;

public class MapUIHandler : MonoBehaviour
{
    [Header("Dependencies")]
    public UIDocument mapUIDoc;
    public MapCameraControl mapCameraControl;

    // Флаги нажатия кнопок
    private bool isW, isA, isS, isD;

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
        // Кнопки управления (PointerDown/Up для удержания)
        SetupButton(root, "BtnW", (v) => isW = v);
        SetupButton(root, "BtnA", (v) => isA = v);
        SetupButton(root, "BtnS", (v) => isS = v);
        SetupButton(root, "BtnD", (v) => isD = v);

        // Кнопка выхода
        var btnExit = root.Q<Button>("BtnExit");
        if (btnExit != null)
        {
            // Удаляем старые, чтобы не дублировать
            btnExit.clicked -= OnExitClicked;
            btnExit.clicked += OnExitClicked;
        }
    }

    // Хелпер для привязки событий нажатия/отпускания
    void SetupButton(VisualElement root, string btnName, System.Action<bool> onStateChange)
    {
        var btn = root.Q<Button>(btnName);
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
    }

    void Update()
    {
        if (mapUIDoc == null || !mapUIDoc.enabled) return;
        if (mapCameraControl == null) return;

        float x = 0f;
        float y = 0f;

        if (isA) x -= 1f;
        if (isD) x += 1f;
        if (isS) y -= 1f;
        if (isW) y += 1f;

        mapCameraControl.SetExternalInput(new Vector2(x, y));
    }
}
