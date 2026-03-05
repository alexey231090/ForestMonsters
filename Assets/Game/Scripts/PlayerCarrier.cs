using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Game.Interfaces;

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
    private IInteractableTrap carriedTrap; // Объект, который мы сейчас несем
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

    public void ProcessHold(GameObject targetObj)
    {
        if (IsCarrying()) return; // Уже заняты руки

        currentHoldTimer += Time.deltaTime;
        
        if (VAR_PickupProgress != null)
            VAR_PickupProgress.Value = currentHoldTimer / holdTimeRequired;

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

    void PerformPickup(GameObject obj)
    {
        // 1. Проверяем, это ЛОВУШКА?
        IInteractableTrap trap = obj.GetComponentInParent<IInteractableTrap>();
        if (trap == null) trap = obj.GetComponentInChildren<IInteractableTrap>();

        if (trap != null)
        {
            if (trap.HasCatch())
            {
                PickUpPhysical(trap);
            }
            else
            {
                if (VAR_TrapsCount != null) VAR_TrapsCount.ApplyChange(1);
                
                // Пытаемся найти самый верхний объект с компонентом ловушки, чтобы удалить всё целиком
                GameObject rootToDestroy = null;
                if (trap is MonoBehaviour trapMono)
                {
                    rootToDestroy = trapMono.gameObject;
                    
                    // Рекурсивно поднимаемся вверх, пока родители тоже являются ловушками
                    // Это решит проблему, если ловушка вложена в другую ловушку/коробку
                    Transform parent = rootToDestroy.transform.parent;
                    while (parent != null && parent.GetComponent<IInteractableTrap>() != null)
                    {
                        rootToDestroy = parent.gameObject;
                        parent = parent.parent;
                    }
                }

                if (rootToDestroy != null)
                {
                    Destroy(rootToDestroy);
                }
                else
                {
                    Destroy(obj);
                }
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

    void PickUpPhysical(IInteractableTrap trap)
    {
        carriedTrap = trap;
        UpdateCarryingFlag();
        trap.OnPickUp(holdPoint);
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
        IInteractableTrap trapToDrop = carriedTrap;
        
        if (trapToDrop is MonoBehaviour trapMono)
        {
            Transform trapTransform = trapMono.transform;
            trapTransform.SetParent(null);

            trapTransform.DOMove(finalPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() =>
            {
                trapToDrop.OnDrop();
            });

            Quaternion targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            trapTransform.DORotateQuaternion(targetRot, 0.5f);
        }

        carriedTrap = null;
        UpdateCarryingFlag();
        Debug.Log("Клетка поставлена!");
    }

    private void UpdateCarryingFlag()
    {
        if (VAR_IsCarrying != null) VAR_IsCarrying.Value = carriedTrap != null;
    }
}
