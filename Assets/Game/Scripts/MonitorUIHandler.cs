using UnityEngine;
using UnityEngine.UIElements;

public class MonitorUIHandler : MonoBehaviour
{
    [Header("UI Toolkit")]
    public UIDocument monitorUIDoc;

    private VisualElement root;
    private Label moneyText;
    private Label infoText;

    void Start()
    {
        if (monitorUIDoc != null && monitorUIDoc.enabled)
        {
            BindUI();
        }
    }

    void Update()
    {
        if (CctvManager.instance != null && CctvManager.instance.isMonitorActive)
        {
            if (!CctvManager.instance.isWatchingCameras && !CctvManager.instance.isWatchingMap && moneyText != null && GameManager.instance != null)
            {
                moneyText.text = $"$ {GameManager.instance.money}";
                if (infoText != null)
                    infoText.text = $"Ловушки: {GameManager.instance.trapsCount} | Камеры: {GameManager.instance.camerasCount}";
            }
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
        if (GameManager.instance != null) GameManager.instance.BuyTrap();
        print("купил ловушку");
    }

    void OnBuyCameraClicked()
    {
        if (GameManager.instance != null) GameManager.instance.BuyCamera();
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
