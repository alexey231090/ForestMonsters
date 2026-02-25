using UnityEngine;

/// <summary>
/// Интерфейс для интерактивных ловушек.
/// Позволяет PlayerInteract работать с любыми ловушками без прямой зависимости от конкретной реализации.
/// </summary>
public interface IInteractableTrap
{
    /// <summary>Можно ли поднять ловушку</summary>
    bool CanBePickedUp { get; }
    
    /// <summary>Вызывается при поднятии ловушки</summary>
    void OnPickUp(Transform hand);
    
    /// <summary>Вызывается при отпускании ловушки</summary>
    void OnDrop();
    
    /// <summary>Есть ли в ловушке пойманный враг</summary>
    bool HasCatch();
}
