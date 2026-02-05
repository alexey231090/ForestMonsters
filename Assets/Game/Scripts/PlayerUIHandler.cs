using UnityEngine;
using UnityEngine.UIElements;

public class PlayerUIHandler : MonoBehaviour
{
    [Header("UI Document")]
    public UIDocument playerUIDoc;

    private VisualElement slotTrap;
    private VisualElement slotCamera;

    private const string CLASS_SELECTED = "selected";
    private VisualElement fuseTrap, fuseCam;
    private VisualElement fuseTrapTop, fuseTrapRight, fuseTrapBottom, fuseTrapLeft;
    private VisualElement fuseCamTop, fuseCamRight, fuseCamBottom, fuseCamLeft;

    void OnEnable()
    {
        // Если уже есть ссылка, пробуем привязать
        if (playerUIDoc != null)
        {
            BindUI();
        }
    }

    void Start()
    {
        if (playerUIDoc == null) playerUIDoc = GetComponent<UIDocument>();
        if (playerUIDoc == null)
        {
            var docs = Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var d in docs)
            {
                var test = d.rootVisualElement.Q<VisualElement>("InventoryContainer");
                if (test != null) { playerUIDoc = d; break; }
            }
        }
        
        BindUI();
    }

    /// <summary>
    /// Показывает HUD и обновляет ссылки на элементы
    /// </summary>
    public void Show()
    {
        if (playerUIDoc != null)
        {
            playerUIDoc.enabled = true;
            BindUI();
        }
    }

    /// <summary>
    /// Скрывает HUD
    /// </summary>
    public void Hide()
    {
        if (playerUIDoc != null) playerUIDoc.enabled = false;
    }

    private void BindUI()
    {
        if (playerUIDoc == null) return;
        var root = playerUIDoc.rootVisualElement;
        if (root == null) return;

        slotTrap = root.Q<VisualElement>("SlotTrap");
        slotCamera = root.Q<VisualElement>("SlotCamera");
        fuseTrap = root.Q<VisualElement>("FuseTrap");
        fuseCam = root.Q<VisualElement>("FuseCam");
        fuseTrapTop = root.Q<VisualElement>("FuseTrapTop");
        fuseTrapRight = root.Q<VisualElement>("FuseTrapRight");
        fuseTrapBottom = root.Q<VisualElement>("FuseTrapBottom");
        fuseTrapLeft = root.Q<VisualElement>("FuseTrapLeft");
        fuseCamTop = root.Q<VisualElement>("FuseCamTop");
        fuseCamRight = root.Q<VisualElement>("FuseCamRight");
        fuseCamBottom = root.Q<VisualElement>("FuseCamBottom");
        fuseCamLeft = root.Q<VisualElement>("FuseCamLeft");
        
        HideAllFuses();
    }

    /// <summary>
    /// Выбирает слот (0 - Ловушка, 1 - Камера, -1 - Ничего)
    /// </summary>
    public void SelectSlot(int index)
    {
        if (slotTrap == null || slotCamera == null) return;

        slotTrap.RemoveFromClassList(CLASS_SELECTED);
        slotCamera.RemoveFromClassList(CLASS_SELECTED);

        if (index == 0) slotTrap.AddToClassList(CLASS_SELECTED);
        else if (index == 1) slotCamera.AddToClassList(CLASS_SELECTED);

        SetFuseActive(index, true);
        SetFuseProgress(index, 1f);
    }

    /// <summary>
    /// Включает/выключает визуал фитиля для указанного слота
    /// </summary>
    public void SetFuseActive(int index, bool active)
    {
        if (fuseTrap == null || fuseCam == null) return;

        // Скрыть все
        fuseTrap.style.display = DisplayStyle.None;
        fuseCam.style.display = DisplayStyle.None;

        if (!active || index < 0) return;

        if (index == 0) fuseTrap.style.display = DisplayStyle.Flex;
        else if (index == 1) fuseCam.style.display = DisplayStyle.Flex;
    }

    /// <summary>
    /// Устанавливает прогресс фитиля (0..1) по периметру: верх -> правая -> низ -> левая
    /// </summary>
    public void SetFuseProgress(int index, float progress01)
    {
        progress01 = Mathf.Clamp01(progress01);
        if (index == 0)
        {
            ApplyFuse(fuseTrapTop, fuseTrapRight, fuseTrapBottom, fuseTrapLeft, progress01);
        }
        else if (index == 1)
        {
            ApplyFuse(fuseCamTop, fuseCamRight, fuseCamBottom, fuseCamLeft, progress01);
        }
    }

    private void ApplyFuse(VisualElement top, VisualElement right, VisualElement bottom, VisualElement left, float p)
    {
        if (top == null || right == null || bottom == null || left == null) return;
        float s = p * 4f;
        float topFrac = Mathf.Clamp01(s);
        float rightFrac = Mathf.Clamp01(s - 1f);
        float bottomFrac = Mathf.Clamp01(s - 2f);
        float leftFrac = Mathf.Clamp01(s - 3f);

        // Верхняя и нижняя линии управляются шириной в процентах
        SetWidthPercent(top, topFrac);
        SetWidthPercent(bottom, bottomFrac);
        // Правая и левая линии управляются высотой в процентах
        SetHeightPercent(right, rightFrac);
        SetHeightPercent(left, leftFrac);
    }

    private void SetWidthPercent(VisualElement el, float frac)
    {
        if (el == null) return;
        if (frac <= 0f)
        {
            el.style.display = DisplayStyle.None;
            return;
        }
        el.style.display = DisplayStyle.Flex;
        el.style.width = new Length(frac * 100f, LengthUnit.Percent);
    }

    private void SetHeightPercent(VisualElement el, float frac)
    {
        if (el == null) return;
        if (frac <= 0f)
        {
            el.style.display = DisplayStyle.None;
            return;
        }
        el.style.display = DisplayStyle.Flex;
        el.style.height = new Length(frac * 100f, LengthUnit.Percent);
    }

    private void HideAllFuses()
    {
        if (fuseTrap != null) fuseTrap.style.display = DisplayStyle.None;
        if (fuseCam != null) fuseCam.style.display = DisplayStyle.None;
    }
}
