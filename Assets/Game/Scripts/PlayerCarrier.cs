using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
    
public class PlayerCarrier : SignalBinder 
{
    [Header("Carry Settings")]
    public Transform holdPoint;
    public float holdTimeRequired = 1.0f;
    public float dropEmbedDepth = 0.2f; // Насколько утапливать при сбросе
    public LayerMask groundLayer;

    [Header("Variables SO")]
    [SerializeField] IntVariable VAR_TrapsCount;
    [SerializeField] IntVariable VAR_CamerasCount;
    [SerializeField] FloatVariable VAR_PickupProgress;
    [SerializeField] BoolVariable VAR_IsCarrying;
    [SerializeField] BoolVariable VAR_IsBuildFuseActive;

    // Внутренние переменные
    private Trap carriedTrap; // Объект, который мы сейчас несем
    private float currentHoldTimer = 0f;

    protected override void OnEnable()
    {
        base.OnEnable();
        ResetHoldTimer();
        UpdateCarryingFlag();
    }

    void Update()
    {
        // Если мы что-то несем, ждем нажатия E для сброса
        if (carriedTrap != null)
        {
            if (VAR_PickupProgress != null) VAR_PickupProgress.Value = 0; // Скрываем круг
            
            if (Input.GetKeyDown(KeyCode.E))
            {
                TryDrop();
            }
        }
        else
        {
            // Если ничего не несем и кнопка E не нажата - сбрасываем таймер
            bool isBuildModeActive = VAR_IsBuildFuseActive != null && VAR_IsBuildFuseActive.Value;
            if (!Input.GetKey(KeyCode.E) && !isBuildModeActive)
            {
                ResetHoldTimer();
            }
        }
    }

    public bool IsCarrying()
    {
        return carriedTrap != null;
    }

    // Вызывается из PlayerInteract каждый кадр, пока мы смотрим на объект и держим E
    public void ProcessHold(GameObject targetObj)
    {
        if (IsCarrying()) return; // Уже заняты руки

        // Увеличиваем таймер
        currentHoldTimer += Time.deltaTime;
        
        // Обновляем SO переменную для UI
        if (VAR_PickupProgress != null)
            VAR_PickupProgress.Value = currentHoldTimer / holdTimeRequired;

        // Если удержали нужное время
        if (currentHoldTimer >= holdTimeRequired)
        {
            PerformPickup(targetObj);
            ResetHoldTimer();
        }
    }

    public void ResetHoldTimer()
    {
        currentHoldTimer = 0f;
        if (VAR_PickupProgress != null) VAR_PickupProgress.Value = 0;
    }

    // Логика: Что делать с объектом (Взять в руки или В инвентарь)
    void PerformPickup(GameObject obj)
    {
        // 1. Проверяем, это ЛОВУШКА?
        Trap trap = obj.GetComponentInParent<Trap>();
        if (trap == null) trap = obj.GetComponentInChildren<Trap>();

        if (trap != null)
        {
            if (trap.HasCatch()) PickUpPhysical(trap);
            else
            {
                if (VAR_TrapsCount != null) VAR_TrapsCount.ApplyChange(1);
                if (trap.trapbox != null) Destroy(trap.trapbox.gameObject);
                else Destroy(trap.gameObject);
            }
            return;
        }

        // 2. Проверяем, это КАМЕРА?
        SecurityCameraSetup camera = obj.GetComponentInParent<SecurityCameraSetup>();
        if (camera == null) camera = obj.GetComponentInChildren<SecurityCameraSetup>();

        if (camera != null)
        {
            if (VAR_CamerasCount != null) VAR_CamerasCount.ApplyChange(1);
            Destroy(camera.gameObject);
            return;
        }
    }

    // --- ФИЗИЧЕСКАЯ ПЕРЕНОСКА (Только для ловушек с добычей) ---

    void PickUpPhysical(Trap trap)
    {
        carriedTrap = trap;
        UpdateCarryingFlag();

        Collider[] cols = trap.GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;

        // Привязываем к рукам
        Transform targetTransform = trap.trapbox != null ? trap.trapbox.transform : trap.transform;
        targetTransform.SetParent(holdPoint);

        // Анимация полета в руки
        targetTransform.DOLocalMove(Vector3.zero, 0.5f);
        targetTransform.DOLocalRotate(Vector3.zero, 0.5f);

        Debug.Log("Клетка с монстром взята!");
    }

    void TryDrop()
    {
        RaycastHit hit;
        if (Physics.Raycast(holdPoint.position, Vector3.down, out hit, 10f, groundLayer))
        {
            DropPhysical(hit.point);
        }
        else
        {
            Debug.Log("Нет земли, чтобы поставить!");
        }
    }

    void DropPhysical(Vector3 floorPos)
    {
        Vector3 finalPos = floorPos - new Vector3(0, dropEmbedDepth, 0);

        Trap trapToDrop = carriedTrap;
        Transform targetTransform = trapToDrop.trapbox != null ? trapToDrop.trapbox.transform : trapToDrop.transform;

        targetTransform.SetParent(null);

        // Анимация падения
        targetTransform.DOMove(finalPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            if (trapToDrop != null)
            {
                Collider[] cols = trapToDrop.GetComponentsInChildren<Collider>();
                foreach (var c in cols) c.enabled = true;
            }
        });

        // Поворот по Y игрока
        Quaternion targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        targetTransform.DORotateQuaternion(targetRot, 0.5f);

        carriedTrap = null; // Руки свободны
        UpdateCarryingFlag();
        Debug.Log("Клетка поставлена!");
    }

    private void UpdateCarryingFlag()
    {
        if (VAR_IsCarrying != null) VAR_IsCarrying.Value = carriedTrap != null;
    }
}

