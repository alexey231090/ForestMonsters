using UnityEngine;
using UnityEngine.UI;

public class HoldProgressBarController : SignalBinder
{
    [Header("UI Reference")]
    [SerializeField] private Image progressBarImage;

    [Header("Variable SO")]
    [SerializeField, Bind] FloatVariable VAR_PickupProgress;
    
    private bool wasActive = false;

    private void OnVAR_PickupProgressChanged()
    {
        if (progressBarImage == null) return;

        float progress = VAR_PickupProgress != null ? VAR_PickupProgress.Value : 0f;
        bool shouldBeActive = progress > 0;
        
        // Обновляем fillAmount всегда
        progressBarImage.fillAmount = progress;
        
        // Вызываем SetActive только если состояние изменилось
        if (wasActive != shouldBeActive)
        {
            progressBarImage.gameObject.SetActive(shouldBeActive);
            wasActive = shouldBeActive;
        }
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        // Найти Image если не назначен
        if (progressBarImage == null)
        {
            progressBarImage = GetComponent<Image>();
        }
        
        // Начальная синхронизация
        OnVAR_PickupProgressChanged();
    }
}
