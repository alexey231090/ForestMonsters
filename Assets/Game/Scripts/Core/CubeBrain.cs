using UnityEngine;

/// <summary>
/// Пример использования SignalBinder.
/// Привязывает 3 события к методам смены цвета куба.
/// Не нужно вешать GlobalEventRelay — подписка автоматическая!
/// </summary>
public class CubeBrain : SignalBinder
{
    [Header("События из папки Assets")]
    public GameEvent evRed;
    public GameEvent evGreen;
    public GameEvent evBlue;

    private MeshRenderer _renderer;

    void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();

        // Привязываем события к методам
        Bind(evRed,   SetRed);
        Bind(evGreen, SetGreen);
        Bind(evBlue,  SetBlue);
    }

    private void SetRed()   => _renderer.material.color = Color.red;
    private void SetGreen() => _renderer.material.color = Color.green;
    private void SetBlue()  => _renderer.material.color = Color.blue;
}
