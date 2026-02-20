using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Classic event listener (legacy system).
/// Subscribes to GameEvent and invokes UnityEvent Response.
/// Maintained for backward compatibility.
/// </summary>
public class GameEventListener : MonoBehaviour
{
    public GameEvent Event;
    public UnityEvent Response;

    private void OnEnable() => Event?.RegisterListener(this);
    private void OnDisable() => Event?.UnregisterListener(this);

    public void OnEventRaised() => Response.Invoke();
}
