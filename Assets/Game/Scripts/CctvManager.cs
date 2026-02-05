using UnityEngine;
using System.Collections.Generic;

public class CctvManager : MonoBehaviour
{
    public static CctvManager instance;
//23
    [Header("Main References")]
    public Camera playerCamera;
    public MonoBehaviour playerController;

    [Header("Old UI")]
    public GameObject cctvViewUI;

    [Header("Map")]
    public Camera mapCamera;
    public MapUIHandler mapUIHandler; // NEW: Ссылка на наш новый UI

    [Header("UI Handler")]
    public MonitorUIHandler uiHandler; // UI Монитора (меню выбора)
    public PlayerUIHandler playerHUD;  // UI Игрока (инвентарь)

    public List<Camera> securityCameras = new List<Camera>();
    public int currentCamIndex = 0;

    public bool isMonitorActive = false;

    public bool isWatchingCameras = false;
    public bool isWatchingMap = false;

    // --- ИСПРАВЛЕНИЕ: Таймеры для защиты от двойного нажатия ---
    private float lastExitTime = -1f;
    private float lastEnterTime = -1f;
    // -----------------------------------------------------------

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        if (mapCamera) mapCamera.enabled = false;
        if (mapUIHandler) mapUIHandler.HideUI(); 
        
        // Пытаемся найти HUD если не назначен
        if (playerHUD == null) playerHUD = FindFirstObjectByType<PlayerUIHandler>();
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

        if (cctvViewUI) cctvViewUI.SetActive(false);
        if (mapCamera) mapCamera.enabled = false;
        
        // Скрываем HUD игрока
        if (playerHUD != null && playerHUD.playerUIDoc != null) playerHUD.playerUIDoc.enabled = false;

        if (uiHandler != null) uiHandler.ShowUI();
    }



    public void SwitchMode(bool menu, bool cams, bool map)
    {
        if (menu && uiHandler != null) uiHandler.ShowUI();
        else if (uiHandler != null) uiHandler.HideUI();
        
        // Управление Map UI
        if (map && mapUIHandler != null) mapUIHandler.ShowUI();
        else if (mapUIHandler != null) mapUIHandler.HideUI();

        if (playerCamera) playerCamera.enabled = menu;
        if (cctvViewUI) cctvViewUI.SetActive(cams);
        if (!cams) foreach (var c in securityCameras) if(c) c.enabled = false;
        if (mapCamera) mapCamera.enabled = map;

        if (menu) { UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true; }
        else { UnityEngine.Cursor.lockState = CursorLockMode.Locked; UnityEngine.Cursor.visible = false; }
        
        // Если мы в режиме карты, курсор нужен для кнопок
        if (map) { UnityEngine.Cursor.lockState = CursorLockMode.None; UnityEngine.Cursor.visible = true; }
    }

    public void ReturnToMenu()
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
        
        // Восстанавливаем HUD игрока
        if (playerHUD != null && playerHUD.playerUIDoc != null) playerHUD.playerUIDoc.enabled = true;

        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        
        lastExitTime = Time.time; // Запоминаем время выхода
    }

   public void ActivateCamera(int index)
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