using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI; // Для работы с UI
using TMPro;

public class CctvManager : MonoBehaviour
{
    public static CctvManager instance;

    [Header("Main References")]
    public Camera playerCamera;
    public MonoBehaviour playerController;

    [Header("UI References")]
    public GameObject monitorMenuUI; // Канвас с кнопками (Главное меню монитора)
    public GameObject cctvViewUI;    // Канвас режима камер (Надписи CAM 1 и т.д.)
    public TMP_Text balanceText;         // Текст внутри меню: "Баланс: $100"

    private List<Camera> securityCameras = new List<Camera>();
    private int currentCamIndex = 0;

    // Состояния
    private bool isMonitorActive = false; // Мы вообще за монитором?
    private bool isWatchingCameras = false; // Мы смотрим в камеры (или в меню)?
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

    void Update()
    {
        // Если мы за монитором
        if (isMonitorActive)
        {
            // Обновляем текст баланса в меню
            if (!isWatchingCameras && balanceText != null && GameManager.instance != null)
            {
                balanceText.text = $"Баланс: ${GameManager.instance.money}\nЛовушек: {GameManager.instance.trapsCount}\nКамер: {GameManager.instance.camerasCount}";
            }

            // Если смотрим камеры - управление A/D
            if (isWatchingCameras)
            {
                if (Input.GetKeyDown(KeyCode.D)) NextCamera();
                if (Input.GetKeyDown(KeyCode.A)) PrevCamera();
            }

            // Выход из всего монитора (ESC или E)
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                // Если мы смотрели камеры, вернемся в меню (опционально) или выйдем совсем
                // Давай сделаем полный выход для простоты, или возврат в меню
                if (isWatchingCameras)
                {
                    ReturnToMenu(); // Вернуться в меню магазина
                }
                else
                {
                    ExitMonitorMode(); // Встать из-за стола
                }
            }
        }
    }

    // --- ВХОД В МОНИТОР (ОТКРЫВАЕТ МЕНЮ) ---
    public void EnterMonitorMode()
    {
        if (lastExitTime > 0f && Time.time - lastExitTime < 0.2f) return;

        isMonitorActive = true;
        isWatchingCameras = false;

        // Отключаем управление игрока
        if (playerController) playerController.enabled = false;

        // Включаем КУРСОР, чтобы жать кнопки
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Показываем меню
        if (monitorMenuUI) monitorMenuUI.SetActive(true);
        if (cctvViewUI) cctvViewUI.SetActive(false);
    }

    // --- КНОПКИ UI ---

    // Кнопка "Камеры"
    public void OnCamerasButtonClicked()
    {
        if (securityCameras.Count == 0)
        {
            Debug.Log("Нет камер!");
            return;
        }

        isWatchingCameras = true;

        // Скрываем меню, скрываем курсор
        if (monitorMenuUI) monitorMenuUI.SetActive(false);
        if (cctvViewUI) cctvViewUI.SetActive(true);

        // Выключаем камеру игрока
        if (playerCamera) playerCamera.enabled = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Включаем первую камеру
        currentCamIndex = 0;
        ActivateCamera(currentCamIndex);
    }

    // Кнопка "Купить Ловушку"
    public void OnBuyTrapClicked()
    {
        if (GameManager.instance != null) GameManager.instance.BuyTrap();
    }

    // Кнопка "Купить Камеру"
    public void OnBuyCameraClicked()
    {
        if (GameManager.instance != null) GameManager.instance.BuyCamera();
    }

    // Кнопка "Выход"
    public void OnExitButtonClicked()
    {
        ExitMonitorMode();
    }

    // --- СЛУЖЕБНЫЕ МЕТОДЫ ---

    void ReturnToMenu()
    {
        isWatchingCameras = false;

        // Выключаем камеры наблюдения
        foreach (Camera cam in securityCameras) if (cam) cam.enabled = false;

        // Включаем камеру игрока (но не управление)
        if (playerCamera) playerCamera.enabled = true;

        // Включаем меню и курсор
        if (monitorMenuUI) monitorMenuUI.SetActive(true);
        if (cctvViewUI) cctvViewUI.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ExitMonitorMode()
    {
        isMonitorActive = false;
        isWatchingCameras = false;

        // Всё UI выключаем
        if (monitorMenuUI) monitorMenuUI.SetActive(false);
        if (cctvViewUI) cctvViewUI.SetActive(false);

        // Выключаем камеры наблюдения
        foreach (Camera cam in securityCameras) if (cam) cam.enabled = false;

        // Включаем игрока
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