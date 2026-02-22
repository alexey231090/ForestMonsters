using UnityEngine;
using UnityEngine.UI;

public class HoldProgressBarController : SignalBinder
{
    [Header("UI Reference")]
    [SerializeField] private Image progressBarImage;

    [Header("Variable SO")]
    [SerializeField, Bind] FloatVariable VAR_PickupProgress;

    private void OnVAR_PickupProgressChanged()
    {
        if (progressBarImage == null) return;

        float progress = VAR_PickupProgress != null ? VAR_PickupProgress.Value : 0f;
        progressBarImage.fillAmount = progress;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        
        if (progressBarImage == null)
        {
            progressBarImage = GetComponent<Image>();
        }
        
        OnVAR_PickupProgressChanged();
    }
}
