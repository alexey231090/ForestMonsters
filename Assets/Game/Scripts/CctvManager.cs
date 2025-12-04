using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class CctvManager : MonoBehaviour
{
    public static CctvManager instance;

    [Header("Main References")]
    public Camera playerCamera;
    public Camera mapCamera; // <--- НОВОЕ: Ссылка на камеру карты
    public MonoBehaviour playerController;

    [Header("UI References")]
    public GameObject monitorMenuUI; // Главное меню
    public GameObject cctvViewUI;    // Просмотр камер (CAM 1...)
    public GameObject mapUI;         // Панель карты (только кнопка НАЗАД)
    public TMP_Text balanceText;

    private List<Camera> securityCameras = new List<Camera>();
    private int currentCamIndex = 0;

    // Состояния
    private bool isMonitorActive = false;
    private bool isWatchingCameras = false;
    private bool isWatchingMap = false;
    private float lastExitTime = -1f;

    void Awake()
    {
        instance = this;
    }

    public void RegisterCamera(Camera newCam)
    {
        newCam.enabled = false;
        var listener = newCam.GetComponent<AudioListener>();
        if (listener) listener.enabled = false;
        securityCameras.Add(newCam);
    }

    void Start()
    {
        // На старте выключаем карту
        if (mapCamera) mapCamera.enabled = false;
    }

    void Update()
    {
        if (isMonitorActive)
        {
            // Обновляем баланс в меню
            if (!isWatchingCameras && !isWatchingMap && balanceText != null && GameManager.instance != null)
            {
                balanceText.text = $"Баланс: ${GameManager.instance.money}\nЛовушек: {GameManager.instance.trapsCount}\nКамер: {GameManager.instance.camerasCount}";
            }

            // Управление камерами (A/D)
            if (isWatchingCameras)
            {
                if (Input.GetKeyDown(KeyCode.D)) NextCamera();
                if (Input.GetKeyDown(KeyCode.A)) PrevCamera();
            }

            // Выход (E или ESC)
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                if (isWatchingCameras || isWatchingMap)
                {
                    ReturnToMenu();
                }
                else
                {
                    ExitMonitorMode();
                }
            }
        }
    }

    public void EnterMonitorMode()
    {
        if (lastExitTime > 0f && Time.time - lastExitTime < 0.2f) return;

        isMonitorActive = true;
        isWatchingCameras = false;
        isWatchingMap = false;

        if (playerController) playerController.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Показываем меню, скрываем остальное
        if (monitorMenuUI) monitorMenuUI.SetActive(true);
        if (cctvViewUI) cctvViewUI.SetActive(false);
        if (mapUI) mapUI.SetActive(false);

        // Камеры выключены
        if (mapCamera) mapCamera.enabled = false;
    }

    // --- КНОПКИ ---

    public void OnMapButtonClicked()
    {
        isWatchingMap = true;

        // 1. UI
        if (monitorMenuUI) monitorMenuUI.SetActive(false);
        if (mapUI) mapUI.SetActive(true); // Включаем кнопку "НАЗАД"

        // 2. Камеры
        if (playerCamera) playerCamera.enabled = false; // Выкл игрока
        if (mapCamera) mapCamera.enabled = true; // ВКЛ КАРТУ
    }

    public void OnBackFromMapClicked()
    {
        ReturnToMenu();
    }

    public void OnCamerasButtonClicked()
    {
        if (securityCameras.Count == 0)
        {
            Debug.Log("Нет камер!");
            return;
        }

        isWatchingCameras = true;

        if (monitorMenuUI) monitorMenuUI.SetActive(false);
        if (cctvViewUI) cctvViewUI.SetActive(true);

        if (playerCamera) playerCamera.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentCamIndex = 0;
        ActivateCamera(currentCamIndex);
    }

    public void OnBuyTrapClicked()
    {
        if (GameManager.instance != null) GameManager.instance.BuyTrap();
    }

    public void OnBuyCameraClicked()
    {
        if (GameManager.instance != null) GameManager.instance.BuyCamera();
    }

    public void OnExitButtonClicked()
    {
        ExitMonitorMode();
    }

    // --- СЛУЖЕБНЫЕ --

    void ReturnToMenu()
    {
        isWatchingCameras = false;
        isWatchingMap = false;

        // Выключаем камеры и UI карты/камер
        foreach (Camera cam in securityCameras) if (cam) cam.enabled = false;
        if (mapCamera) mapCamera.enabled = false; // ВЫКЛ КАРТУ

        if (cctvViewUI) cctvViewUI.SetActive(false);
        if (mapUI) mapUI.SetActive(false);

        // Включаем меню
        if (monitorMenuUI) monitorMenuUI.SetActive(true);

        // Включаем игрока (визуально), курсор
        if (playerCamera) playerCamera.enabled = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitMonitorMode()
    {
        isMonitorActive = false;
        isWatchingCameras = false;
        isWatchingMap = false;

        if (monitorMenuUI) monitorMenuUI.SetActive(false);
        if (cctvViewUI) cctvViewUI.SetActive(false);
        if (mapUI) mapUI.SetActive(false);

        foreach (Camera cam in securityCameras) if (cam) cam.enabled = false;
        if (mapCamera) mapCamera.enabled = false;

        if (playerCamera) playerCamera.enabled = true;
        if (playerController) playerController.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        lastExitTime = Time.time;
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