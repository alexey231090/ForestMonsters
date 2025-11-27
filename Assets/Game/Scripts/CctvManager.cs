using UnityEngine;
using System.Collections.Generic;

public class CctvManager : MonoBehaviour
{
    public static CctvManager instance; // �������� ��� �������� �������
    [Header("Description")]
    [TextArea(2,5)] public string description = "Менеджер CCTV: вход/выход из монитора, переключение камер (A/D), показ UI.";

    [Header("Main")]
    public Camera playerCamera;      // ������ �� ������ ������
    public MonoBehaviour playerController; // ������ �� ������ �������� ������
    public GameObject monitorUI;     // UI � ��������� (���� ����)

    // ������ �����, ������� ����������� �������������
    private List<Camera> securityCameras = new List<Camera>();

    private int currentCamIndex = 0;
    private bool isMonitorActive = false;
    private float lastExitTime = -1f;

    public bool IsMonitorActive()
    {
        return isMonitorActive;
    }

    void Awake()
    {
        // ������������� ���������
        instance = this;
    }

    // ����� ����������� ����� ������ (���������� �� SecurityCameraSetup)
    public void RegisterCamera(Camera newCam)
    {
        // ����� ��������� ������, ����� ��� �� ���������� ���
        newCam.enabled = false;

        // ��������� ���� �� ������ (Unity �� ����� 2 ���������)
        var listener = newCam.GetComponent<AudioListener>();
        if (listener) listener.enabled = false;

        securityCameras.Add(newCam);
        Debug.Log("������ ����������������! �����: " + securityCameras.Count);
    }

    void Update()
    {
        // ���������� �������� ������ � ������ ��������
        if (isMonitorActive)
        {
            if (Input.GetKeyDown(KeyCode.D)) NextCamera();
            if (Input.GetKeyDown(KeyCode.A)) PrevCamera();

            // �����
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))
            {
                ExitMonitorMode();
            }
        }
    }

    public void EnterMonitorMode()
    {
        if (lastExitTime > 0f && Time.time - lastExitTime < 0.2f) return;
        if (securityCameras.Count == 0)
        {
            Debug.Log("��� ������������� �����!");
            return;
        }

        isMonitorActive = true;

        // 1. ��������� ������
        if (playerCamera)
        {
            playerCamera.enabled = false;
            var pListener = playerCamera.GetComponent<AudioListener>();
            if (pListener) pListener.enabled = false;
        }

        if (playerController) playerController.enabled = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 2. �������� ������ ������ �� ������
        currentCamIndex = 0;
        ActivateCamera(currentCamIndex);

        if (monitorUI) monitorUI.SetActive(true);
    }

    public void ExitMonitorMode()
    {
        isMonitorActive = false;

        // 1. ��������� ��� �������� ������
        foreach (Camera cam in securityCameras)
        {
            if (cam != null) cam.enabled = false;
        }

        // 2. ���������� ���������� ������
        if (playerCamera)
        {
            playerCamera.enabled = true;
            var pListener = playerCamera.GetComponent<AudioListener>();
            if (pListener) pListener.enabled = true;
        }

        if (playerController) playerController.enabled = true;

        if (monitorUI) monitorUI.SetActive(false);
        lastExitTime = Time.time;
    }

    void ActivateCamera(int index)
    {
        for (int i = 0; i < securityCameras.Count; i++)
        {
            if (securityCameras[i] != null)
            {
                securityCameras[i].enabled = (i == index);
            }
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
