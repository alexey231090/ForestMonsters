using UnityEngine;

public class SecurityCameraSetup : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2,5)] public string description = "Настройка охранной камеры: находит дочернюю Camera и регистрирует её в CctvManager.";
    void Start()
    {
        // ���� ��������� Camera � ���� ������� ��� ��� �����
        Camera myCam = GetComponentInChildren<Camera>();

        // ���������� � CctvManager 
        if (myCam != null && CctvManager.instance != null)
        {
            CctvManager.instance.RegisterCamera(myCam);
        }
        else
        {
            if (myCam == null) Debug.LogWarning("�� ������� SecurityCamera ��� ���������� Camera!");
        }
    }
}
