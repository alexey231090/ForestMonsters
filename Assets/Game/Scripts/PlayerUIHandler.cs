using UnityEngine;
using UnityEngine.UIElements;

public class PlayerUIHandler : SignalBinder
{
    [Header("UI Document")]
    public UIDocument playerUIDoc;

    [Header("Observed Variables")]
    [SerializeField, Bind("UpdateSelection")] private IntVariable VAR_SelectedSlot;
    [SerializeField, Bind("UpdateFuseVisuals")] private FloatVariable VAR_BuildFuseProgress;
    [SerializeField, Bind("UpdateFuseVisuals")] private BoolVariable VAR_IsBuildFuseActive;

    private VisualElement slotTrap;
    private VisualElement slotCamera;

    private const string CLASS_SELECTED = "selected";
    private VisualElement fuseTrap, fuseCam;
    private VisualElement fuseTrapTop, fuseTrapRight, fuseTrapBottom, fuseTrapLeft;
    private VisualElement fuseCamTop, fuseCamRight, fuseCamBottom, fuseCamLeft;

    protected override void OnEnable()
    {
        base.OnEnable();
        
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
        
        // Initial sync
        UpdateSelection();
        UpdateFuseVisuals();
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

    private void UpdateSelection()
    {
        if (slotTrap == null || slotCamera == null) return;

        int index = VAR_SelectedSlot != null ? VAR_SelectedSlot.Value : -1;

        slotTrap.RemoveFromClassList(CLASS_SELECTED);
        slotCamera.RemoveFromClassList(CLASS_SELECTED);

        if (index == 0) slotTrap.AddToClassList(CLASS_SELECTED);
        else if (index == 1) slotCamera.AddToClassList(CLASS_SELECTED);
    }

    private void UpdateFuseVisuals()
    {
        if (fuseTrap == null || fuseCam == null) return;

        bool active = VAR_IsBuildFuseActive != null && VAR_IsBuildFuseActive.Value;
        float progress01 = VAR_BuildFuseProgress != null ? VAR_BuildFuseProgress.Value : 0f;
        int index = VAR_SelectedSlot != null ? VAR_SelectedSlot.Value : -1;

        // Reset visibility
        fuseTrap.style.display = DisplayStyle.None;
        fuseCam.style.display = DisplayStyle.None;

        if (!active || index < 0) return;

        if (index == 0) 
        {
            fuseTrap.style.display = DisplayStyle.Flex;
            ApplyFuse(fuseTrapTop, fuseTrapRight, fuseTrapBottom, fuseTrapLeft, progress01);
        }
        else if (index == 1) 
        {
            fuseCam.style.display = DisplayStyle.Flex;
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

        SetWidthPercent(top, topFrac);
        SetWidthPercent(bottom, bottomFrac);
        SetHeightPercent(right, rightFrac);
        SetHeightPercent(left, leftFrac);
    }

    private void SetWidthPercent(VisualElement el, float frac)
    {
        if (el == null) return;
        el.style.display = frac <= 0 ? DisplayStyle.None : DisplayStyle.Flex;
        el.style.width = new Length(frac * 100f, LengthUnit.Percent);
    }

    private void SetHeightPercent(VisualElement el, float frac)
    {
        if (el == null) return;
        el.style.display = frac <= 0 ? DisplayStyle.None : DisplayStyle.Flex;
        el.style.height = new Length(frac * 100f, LengthUnit.Percent);
    }

    private void HideAllFuses()
    {
        if (fuseTrap != null) fuseTrap.style.display = DisplayStyle.None;
        if (fuseCam != null) fuseCam.style.display = DisplayStyle.None;
    }

    public void Show()
    {
        if (playerUIDoc != null) 
        {
            playerUIDoc.enabled = true;
            // Перепривязываем UI после включения - элементы могли стать null
            BindUI();
            // Принудительно обновляем визуалы
            UpdateSelection();
            UpdateFuseVisuals();
        }
    }

    public void Hide()
    {
        if (playerUIDoc != null) playerUIDoc.enabled = false;
    }
}
