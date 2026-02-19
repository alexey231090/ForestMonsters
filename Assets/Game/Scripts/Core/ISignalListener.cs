/// <summary>
/// Интерфейс для объектов, способных принимать сигналы от GameEvent.
/// Реализуется SmartListener'ами и GlobalEventRelay.
/// </summary>
public interface ISignalListener
{
    void OnSignalReceived(GameEvent incomingEvent);
}
