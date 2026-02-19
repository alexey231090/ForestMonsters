using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Классический слушатель событий (старая система).
/// Подписывается на GameEvent и вызывает UnityEvent Response.
/// Сохранён для обратной совместимости.
/// </summary>
public class GameEventListener : MonoBehaviour
{
    public GameEvent Event;
    public UnityEvent Response;

    private void OnEnable() => Event?.RegisterListener(this);
    private void OnDisable() => Event?.UnregisterListener(this);

    public void OnEventRaised() => Response.Invoke();
}
