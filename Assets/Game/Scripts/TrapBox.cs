using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;
using Game.Interfaces;

public class TrapBox : MonoBehaviour, IInteractableTrap
{
    [TextArea(5, 5)] public string description = "TrapBox - колайдер клетки, логика ловушки и доставка.";

    [Header("Settings")]
    public float attractionDuration = 1f;
    public float pickUpDuration = 0.5f; // Скорость подъема к рукам
    public float dropDuration = 0.5f;   // Скорость опускания на землю

    [SerializeField, Bind] IntVariable VAR_TrapsCount;

    [Header("Catch Positioning")]
    [Tooltip("Custom vertical offset to fine-tune monster position in the cage")]
    public float catchVisualOffset = 0f;

    [Header("References")]
    public Animator animatorCell;
    public Transform captureCenterPoint;
    public ParticleSystem captureParticles;
    public GameObject activeVisual;
    public TrapBox trapbox; // Ссылка на родительский объект (сама коробка)
    public Collider mainPhysicalCollider; // Физический коллайдер клетки (без триггера)
    public Collider catchTriggerCollider; // Триггерный коллайдер для ловли врага

    private bool isUsed = false;
    private bool isDelivered = false; // Флаг для защиты от двойной доставки
    private GameObject caughtEnemy;
    private StringVariable caughtMonsterData; // Данные о виде пойманного монстра
    private Transform trapRoot;

    // IInteractableTrap Implementation
    public bool CanBePickedUp => !isDelivered; // Можно поднять если не доставлена

    void Start()
    {
        if (activeVisual != null) activeVisual.SetActive(false);
        if (animatorCell == null) animatorCell = GetComponentInChildren<Animator>();
        if (trapbox == null) trapbox = GetComponentInParent<TrapBox>();
        trapRoot = transform.parent != null ? transform.parent : transform;

        // Настройка основного физического коллайдера
        if (mainPhysicalCollider != null)
        {
            mainPhysicalCollider.isTrigger = false;
            mainPhysicalCollider.enabled = false; // Выключен, пока никто не пойман
        }

        // Настройка триггера ловушки
        if (catchTriggerCollider != null)
        {
            catchTriggerCollider.isTrigger = true;
            catchTriggerCollider.enabled = true;
        }
        else
        {
            Debug.LogWarning("[TRAP] Catch Trigger Collider is not assigned in Inspector!", this);
        }

        // В Unity триггеры/коллизии вызывают события только если хотя бы на одном из объектов есть Rigidbody.
        // У врагов может не быть Rigidbody (например, только NavMeshAgent), поэтому обеспечиваем его на ловушке.
        var rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TRAP] Trigger entered by: {other.name} with tag: {other.tag}", this);

        // Проверяем доставку в парк
        if (other.CompareTag("ParkTrigger"))
        {
            if (HasCatch() && !isDelivered)
            {
                if (ParkManager.instance != null && ParkManager.instance.TryDeliverMonster(caughtMonsterData))
                {
                    isDelivered = true; // Мгновенно блокируем повторный вход
                    Debug.Log("[TRAP] Monster delivered! Returning trap to inventory.");

                    if (VAR_TrapsCount != null) VAR_TrapsCount.ApplyChange(1);

                    Destroy(trapRoot != null ? trapRoot.gameObject : gameObject);
                }
            }
            return;
        }

        // Логика поимки врага (только если ловушка еще пуста)
        if (isUsed) return;

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Враг попался");
            var enemyAI = other.GetComponent<EnemyAi>();
            if (enemyAI != null)
            {
                enemyAI.IsCaught = true;
                enemyAI.enabled = false;
                var agent = other.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                if (captureCenterPoint != null)
                {
                    // Самый простой и надежный способ: центр + оффсет
                    Vector3 targetPos = captureCenterPoint.position + (Vector3.up * catchVisualOffset);

                    other.transform.DOMove(targetPos, attractionDuration);
                    other.transform.DORotateQuaternion(captureCenterPoint.rotation, attractionDuration)
                        .OnComplete(() => other.transform.SetParent(captureCenterPoint));
                }

                if (captureParticles != null) captureParticles.Play();
                if (animatorCell != null) animatorCell.SetBool("CellOpenClose", true);

                // Включаем физический коллайдер на коробке при поимке
                if (mainPhysicalCollider != null) mainPhysicalCollider.enabled = true;

                // Запоминаем данные о монстре
                caughtMonsterData = enemyAI.monsterData;

                caughtEnemy = other.gameObject;
                isUsed = true;
            }
        }
    }

    public bool HasCatch()
    {
        return isUsed && caughtEnemy != null;
    }

    // --- ЛОГИКА ПЕРЕНОСКИ (IInteractableTrap) ---

    public void OnPickUp(Transform hand)
    {
        // Отключаем триггер ловли на время переноски
        if (catchTriggerCollider != null) catchTriggerCollider.enabled = false;

        if (trapRoot == null) trapRoot = transform;
        trapRoot.SetParent(hand);
        trapRoot.DOLocalMove(Vector3.zero, pickUpDuration);
        trapRoot.DOLocalRotate(Vector3.zero, pickUpDuration);

        // Включаем эффект призрака
        if (caughtEnemy != null && caughtEnemy.TryGetComponent<IGhostable>(out var ghostable))
        {
            ghostable.SetGhostMode(true);
        }
    }

    public void OnDrop()
    {
        // Вызывается ПОСЛЕ того как PlayerCarrier анимированно поставил объект
        if (catchTriggerCollider != null)
        {
            catchTriggerCollider.enabled = true;
        }

        // Выключаем эффект призрака
        if (caughtEnemy != null && caughtEnemy.TryGetComponent<IGhostable>(out var ghostable))
        {
            ghostable.SetGhostMode(false);
        }
    }

    // Старый метод оставлен для обратной совместимости, если нужен прямой зазов
    public void AnimateDrop(Vector3 targetPosition, Quaternion targetRotation)
    {
        if (trapRoot == null) trapRoot = transform;
        trapRoot.SetParent(null);
        trapRoot.DOMove(targetPosition, dropDuration).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            OnDrop();
        });
        trapRoot.DORotateQuaternion(targetRotation, dropDuration);
    }
}
