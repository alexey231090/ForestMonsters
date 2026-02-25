using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

[RequireComponent(typeof(Rigidbody))]
public class Trap2 : MonoBehaviour
{
    [Header("Коллайдеры (Перетащить из иерархии)")]
    [Tooltip("Обычный физический коллайдер КЛЕТКИ. Должен быть БЕЗ галочки isTrigger.")]
    public Collider physicalCollider;
    
    [Header("Настройки Сферы Обнаружения")]
    public float detectionRadius = 1.0f;
    public Vector3 sphereOffset = Vector3.up * 0.5f;
    public LayerMask detectionLayer;
    [Tooltip("Как часто проверять сферу (в секундах). 0.1 - хороший баланс.")]
    public float checkInterval = 0.1f;

    [Header("Точки и Визуал")]
    public Transform capturePoint;
    public Animator animatorCell;
    public ParticleSystem captureParticles;

    [Header("Настройки")]
    public float attractionSpeed = 0.5f;
    [SerializeField, Bind] private IntVariable VAR_TrapsCount;

    private bool isUsed = false;
    private bool isDelivered = false;
    private bool isActive = true;
    private GameObject caughtEnemy;
    private Rigidbody rb;
    private float nextCheckTime;

    void Awake()
    {
        // Настройка Rigidbody
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        // Первоначальное состояние коллайдеров
        if (physicalCollider != null)
        {
            physicalCollider.isTrigger = false;
            physicalCollider.enabled = false; // Выключен, пока клетка пуста
        }

        Debug.Log($"<color=cyan>[Trap2]</color> Инициализирована на {gameObject.name}. Использование OverlapSphere для обнаружения.");
    }

    void Update()
    {
        if (!isActive || isDelivered) return;

        // Оптимизированная проверка по таймеру
        if (Time.time >= nextCheckTime)
        {
            CheckOverlap();
            nextCheckTime = Time.time + checkInterval;
        }
    }

    private void CheckOverlap()
    {
        Vector3 sphereCenter = transform.TransformPoint(sphereOffset);
        // Находим все коллайдеры в радиусе сферы на заданных слоях
        Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, detectionRadius, detectionLayer);

        foreach (var hitCollider in hitColliders)
        {
            // 1. Логика доставки в парк
            if (hitCollider.CompareTag("ParkTrigger"))
            {
                if (isUsed && caughtEnemy != null && !isDelivered)
                {
                    DeliverToPark();
                    break;
                }
            }

            // 2. Логика ловли врага
            if (!isUsed && hitCollider.CompareTag("Enemy"))
            {
                EnemyAi enemyAI = hitCollider.GetComponent<EnemyAi>();
                // ПРОВЕРКА: Ловим только если враг еще не пойман другой ловушкой
                if (enemyAI != null && !enemyAI.IsCaught)
                {
                    TryCatchEnemy(hitCollider, enemyAI);
                    break;
                }
            }
        }
    }

    private void TryCatchEnemy(Collider enemyCollider, EnemyAi enemyAI)
    {
        Debug.Log($"<color=green>[Trap2]</color> Враг {enemyCollider.name} обнаружен сферой!");
        
        isUsed = true;
        enemyAI.IsCaught = true; // Помечаем врага как пойманного
        enemyAI.enabled = false;

        // Отключаем навигацию врага
        if (enemyCollider.TryGetComponent<NavMeshAgent>(out var agent)) agent.enabled = false;
        if (enemyCollider.TryGetComponent<Rigidbody>(out var enemyRb)) enemyRb.isKinematic = true;

        // Анимация затягивания в центр
        if (capturePoint != null)
        {
            enemyCollider.transform.DOMove(capturePoint.position, attractionSpeed);
            enemyCollider.transform.DORotateQuaternion(capturePoint.rotation, attractionSpeed)
                .OnComplete(() => enemyCollider.transform.SetParent(capturePoint));
        }

        // Визуал
        if (captureParticles != null) captureParticles.Play();
        if (animatorCell != null) animatorCell.SetBool("CellOpenClose", true);

        // ВКЛЮЧАЕМ физический коллайдер
        if (physicalCollider != null) physicalCollider.enabled = true;

        caughtEnemy = enemyCollider.gameObject;
        Debug.Log("<color=green>[Trap2]</color> Враг успешно пойман и заперт!");
    }

    private void DeliverToPark()
    {
        if (ParkManager.instance != null && ParkManager.instance.TryDeliverMonster())
        {
            isDelivered = true;
            Debug.Log("<color=blue>[Trap2]</color> Монстр доставлен в парк!");

            if (VAR_TrapsCount != null) VAR_TrapsCount.ApplyChange(1);

            // Удаляем ловушку
            Destroy(gameObject);
        }
    }

    // Методы для скрипта игрока
    public bool HasCatch() => isUsed && caughtEnemy != null;

    public void OnPickUp(Transform hand)
    {
        isActive = false; // Отключаем проверку сферы при переносе
        transform.SetParent(hand);
        transform.DOLocalMove(Vector3.zero, 0.3f);
        transform.DOLocalRotate(Vector3.zero, 0.3f);
    }

    public void OnDrop()
    {
        transform.SetParent(null);
        isActive = true; // Включаем проверку сферы обратно
    }

    private void OnDrawGizmos()
    {
        // Визуализация сферы в Scene View
        // Зеленая - ищет врага, Красная - готова к сдаче в парк
        Gizmos.color = isUsed ? new Color(1, 0, 0, 0.3f) : new Color(0, 1, 0, 0.3f);
        Vector3 sphereCenter = transform.TransformPoint(sphereOffset);
        Gizmos.DrawSphere(sphereCenter, detectionRadius);
        
        Gizmos.color = isUsed ? Color.red : Color.green;
        Gizmos.DrawWireSphere(sphereCenter, detectionRadius);
    }
}
