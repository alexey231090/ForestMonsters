using UnityEngine;

public class Flashlight : SignalBinder
{
    [Header("Subscribed Events")]
    public GameEvent EV_MonitorEntered;
    public GameEvent EV_MonitorExited;

    private Light myLight;
    public AudioSource clickSound;
    
    private bool _isMonitorActive = false;
    private bool _wasActiveBeforeMonitor = false;

    void Start()
    {
        myLight = GetComponent<Light>();
        
        // Подписываемся на события монитора через нашу систему сигналов
        Bind(EV_MonitorEntered, OnMonitorEntered);
        Bind(EV_MonitorExited, OnMonitorExited);
    }

    void Update()
    {
        // Не даем переключать фанарик, если мы в мониторе.
        if (_isMonitorActive) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleFlashlight(!myLight.enabled);
        }
    }

    private void ToggleFlashlight(bool state)
    {
        if (myLight) myLight.enabled = state;
        if (clickSound) clickSound.Play();
    }

    private void OnMonitorEntered()
    {
        _isMonitorActive = true;
        
        // Запоминаем состояние и выключаем фанарик
        if (myLight)
        {
            _wasActiveBeforeMonitor = myLight.enabled;
            myLight.enabled = false;
        }
    }

    private void OnMonitorExited()
    {
        _isMonitorActive = false;
        
        // Восстанавливаем состояние, которое было до входа в монитор
        if (myLight)
        {
            myLight.enabled = _wasActiveBeforeMonitor;
        }
    }
}
