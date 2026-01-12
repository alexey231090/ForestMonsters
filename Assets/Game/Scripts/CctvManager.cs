using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class CctvManager : MonoBehaviour
{
    public static CctvManager instance;

    [Header("Main References")]
    public Camera playerCamera;
    public MonoBehaviour playerController;
    
    [Header("UI Toolkit")]
    public UIDocument monitorUIDoc; 
    
    [Header("Old UI")]
    public GameObject cctvViewUI;   
    
    [Header("Map")]
    public Camera mapCamera;        

    private List<Camera> securityCameras = new List<Camera>();
    private int currentCamIndex = 0;
    
    public bool isMonitorActive = false; 
    
    private bool isWatchingCameras = false; 
    private bool isWatchingMap = false; 
    
    // --- ИСПРАВЛЕНИЕ: Таймеры для защиты от двойного нажатия ---
    private float lastExitTime = -1f;
    private float lastEnterTime = -1f; 
    // -----------------------------------------------------------

    private VisualElement root;
    private Label moneyText;
    private Label infoText;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (monitorUIDoc != null)
        {
            root = monitorUIDoc.rootVisualElement;
            monitorUIDoc.gameObject.SetActive(false); 

            var btnCameras = root.Q<Button>("BtnCameras");
            var btnTrap = root.Q<Button>("BtnBuyTrap");
            var btnCam = root.Q<Button>("BtnBuyCam");
            var btnMap = root.Q<Button>("BtnMap");
            var btnExit = root.Q<Button>("BtnExit");

            moneyText = root.Q<Label>("MoneyText");
            infoText = root.Q<Label>("InfoText");

            if(btnCameras != null) btnCameras.clicked += OnCamerasButtonClicked;
            if(btnTrap != null)    btnTrap.clicked += OnBuyTrapClicked;
            if(btnCam != null)     btnCam.clicked += OnBuyCameraClicked;
            if(btnMap != null)     btnMap.clicked += OnMapButtonClicked;
            if(btnExit != null)    btnExit.clicked += OnExitButtonClicked;
        }

        if (mapCamera) mapCamera.enabled = false;
    }

    public void RegisterCamera(Camera newCam)
    {
        newCam.enabled = false;
        var listener = newCam.GetComponent<AudioListener>();
        if (listener) listener.enabled = false;
        securityCameras.Add(newCam);
    }

    void Update()
    {
        if (isMonitorActive)
        {
            if (!isWatchingCameras && !isWatchingMap && moneyText != null && GameManager.instance != null)
            {
                moneyText.text = $"$ {GameManager.instance.money}";
                if (infoText != null)
                    infoText.text = $"Traps: {GameManager.instance.trapsCount} | Cams: {GameManager.instance.camerasCount}";
            }

            if (isWatchingCameras)
            {
                if (Input.GetKeyDown(KeyCode.D)) NextCamera();
                if (Input.GetKeyDown(KeyCode.A)) PrevCamera();
            }

            // --- ИСПРАВЛЕНИЕ: Проверяем, прошло ли время после входа ---
            if (Time.time - lastEnterTime > 0.2f) // Ждем 0.2 сек перед тем как разрешить выход
            {
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
                {
                    if (isWatchingCameras || isWatchingMap) ReturnToMenu();
                    else ExitMonitorMode();
                }
            }
            // -----------------------------------------------------------
        }
    }

    public void EnterMonitorMode()
    {
        // Защита от слишком частого входа/выхода
        if (Time.time - lastExitTime < 0.2f) return;

        // --- ИСПРАВЛЕНИЕ: Запоминаем время входа ---
        lastEnterTime = Time.time;
        // -------------------------------------------

        isMonitorActive = true;
        isWatchingCameras = false;
        isWatchingMap = false;

        if (playerController) playerController.enabled = false;
        
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;

        if (monitorUIDoc) monitorUIDoc.gameObject.SetActive(true);
        if (cctvViewUI) cctvViewUI.SetActive(false);
        if (mapCamera) mapCamera.enabled = false;
    }

    // ... (Остальной код кнопок и переключений без изменений) ...

    void OnCamerasButtonClicked()
    {
        if (securityCameras.Count == 0) { Debug.Log("Нет камер!"); return; }
        isWatchingCameras = true;
        SwitchMode(false, true, false); 
        currentCamIndex = 0;
        ActivateCamera(currentCamIndex);
    }

    void OnMapButtonClicked()
    {
        isWatchingMap = true;
        SwitchMode(false, false, true); 
    }

    void OnBuyTrapClicked() { if (GameManager.instance != null) GameManager.instance.BuyTrap(); }
    void OnBuyCameraClicked() { if (GameManager.instance != null) GameManager.instance.BuyCamera(); }
    void OnExitButtonClicked() { ExitMonitorMode(); }

    void SwitchMode(bool menu, bool cams, bool map)
    {
        if (monitorUIDoc) monitorUIDoc.gameObject.SetActive(menu);
        if (playerCamera) playerCamera.enabled = menu; 
        if (cctvViewUI) cctvViewUI.SetActive(cams);
        if (!cams) foreach (var c in securityCameras) if(c) c.enabled = false;
        if (mapCamera) mapCamera.enabled = map;

        if (menu) { UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true; }
        else { UnityEngine.Cursor.lockState = CursorLockMode.Locked; UnityEngine.Cursor.visible = false; }
    }

    void ReturnToMenu()
    {
        isWatchingCameras = false;
        isWatchingMap = false;
        SwitchMode(true, false, false);
    }

    public void ExitMonitorMode()
    {
        isMonitorActive = false;
        SwitchMode(false, false, false);

        if (playerCamera) playerCamera.enabled = true;
        if (playerController) playerController.enabled = true;
        
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        
        lastExitTime = Time.time; // Запоминаем время выхода
    }

    void ActivateCamera(int index)
    {
        for (int i = 0; i < securityCameras.Count; i++)
        {
            if (securityCameras[i] != null) securityCameras[i].enabled = (i == index);
        }
    }

    void NextCamera()
    {
        currentCamIndex++;
        if (currentCamIndex >= securityCameras.Count) currentCamIndex = 0;
        ActivateCamera(currentCamIndex);
    }

    void PrevCamera()
    {
        currentCamIndex--;
        if (currentCamIndex < 0) currentCamIndex = securityCameras.Count - 1;
        ActivateCamera(currentCamIndex);
    }
}