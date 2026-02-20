using UnityEngine;
// Trigger recompile
using UnityEngine.UIElements;

public class MonitorUIHandler : SignalBinder
{
    [Header("UI Toolkit")]
    public UIDocument monitorUIDoc;

    [Header("Variables SO (Display)")]
    [SerializeField, OnChanged(nameof(RefreshUI))] private FloatVariable VAR_Money;
    [SerializeField, OnChanged(nameof(RefreshUI))] private IntVariable VAR_TrapsCount;
    [SerializeField, OnChanged(nameof(RefreshUI))] private IntVariable VAR_CamerasCount;

    [Header("Shop Settings")]
    public float trapPrice = 20f;
    public float cameraPrice = 15f;

    private VisualElement root;
    private Label moneyText;
    private Label infoText;

    void Start()
    {
        if (monitorUIDoc != null && monitorUIDoc.enabled)
        {
            BindUI();
            RefreshUI();
        }
    }

    // OnEnable больше не нужен, SignalBinder сам подпишет переменные через OnChanged!
    
    private void RefreshUI()
    {
        // Не обновляем UI, если монитор не активен или игрок смотрит в камеры/карту
        if (CctvManager.instance == null || !CctvManager.instance.isMonitorActive) return;
        if (CctvManager.instance.isWatchingCameras || CctvManager.instance.isWatchingMap) return;
        
        if (moneyText != null && VAR_Money != null)
        {
            moneyText.text = $"$ {VAR_Money.Value}";
        }
        
        if (infoText != null && VAR_TrapsCount != null && VAR_CamerasCount != null)
        {
            infoText.text = $"Ловушки: {VAR_TrapsCount.Value} | Камеры: {VAR_CamerasCount.Value}";
        }
    }

    void OnCamerasButtonClicked()
    {
        if (CctvManager.instance != null)
        {
            if (CctvManager.instance.securityCameras.Count == 0) { Debug.Log("Нет камер!"); return; }
            CctvManager.instance.isWatchingCameras = true;
            CctvManager.instance.SwitchMode(false, true, false);
            CctvManager.instance.currentCamIndex = 0;
            CctvManager.instance.ActivateCamera(CctvManager.instance.currentCamIndex);
        }
    }

    void OnMapButtonClicked()
    {
        if (CctvManager.instance != null)
        {
            CctvManager.instance.isWatchingMap = true;
            CctvManager.instance.SwitchMode(false, false, true);
        }
        print("Карта");
    }

    void OnBuyTrapClicked()
    {
        if (VAR_Money != null && VAR_TrapsCount != null && VAR_Money.Value >= trapPrice)
        {
            VAR_Money.ApplyChange(-trapPrice);
            VAR_TrapsCount.ApplyChange(1);
            print("купил ловушку");
        }
        else
        {
            print("Недостаточно денег!");
        }
    }

    void OnBuyCameraClicked()
    {
        if (VAR_Money != null && VAR_CamerasCount != null && VAR_Money.Value >= cameraPrice)
        {
            VAR_Money.ApplyChange(-cameraPrice);
            VAR_CamerasCount.ApplyChange(1);
            print("купил камеру");
        }
        else
        {
            print("Недостаточно денег!");
        }
    }

    void OnExitButtonClicked()
    {
        if (CctvManager.instance != null) CctvManager.instance.ExitMonitorMode();
        monitorUIDoc.enabled = false;
        print("Exit");
    }

    public void ShowUI()
    {
        if (monitorUIDoc != null)
        {
            monitorUIDoc.enabled = true;
            BindUI();
            RefreshUI(); // Принудительно обновляем текст при открытии монитора
        }
    }

    public void HideUI()
    {
        if (monitorUIDoc != null) monitorUIDoc.enabled = false;
    }

    void BindUI()
    {
        root = monitorUIDoc.rootVisualElement;
        var btnCameras = root.Q<Button>("BtnCameras");
        var btnTrap = root.Q<Button>("BtnBuyTrap");
        var btnCam = root.Q<Button>("BtnBuyCam");
        var btnMap = root.Q<Button>("BtnMap");
        var btnExit = root.Q<Button>("BtnExit");

        moneyText = root.Q<Label>("MoneyText");
        infoText = root.Q<Label>("InfoText");

        if (btnCameras != null) btnCameras.clicked += OnCamerasButtonClicked;
        if (btnTrap != null) btnTrap.clicked += OnBuyTrapClicked;
        if (btnCam != null) btnCam.clicked += OnBuyCameraClicked;
        if (btnMap != null) btnMap.clicked += OnMapButtonClicked;
        if (btnExit != null) btnExit.clicked += OnExitButtonClicked;
    }
}
