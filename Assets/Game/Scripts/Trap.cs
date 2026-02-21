using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class Trap : MonoBehaviour
{
    [Header("Settings")]
    public float attractionDuration = 1f;
    public float pickUpDuration = 0.5f; // Скорость подъема к рукам
    public float dropDuration = 0.5f;   // Скорость опускания на землю

    [SerializeField, Bind] IntVariable VAR_TrapsCount;

    [Header("References")]
    public Animator animatorCell;
    public Transform captureCenterPoint;
    public ParticleSystem captureParticles;
    public GameObject activeVisual;
    public TrapBox trapbox; // Ссылка на родительский объект (сама коробка)

    private bool isUsed = false;
    private bool isDelivered = false; // Флаг для защиты от двойной доставки
    private GameObject caughtEnemy;
    private Collider myCollider;

    void Start()
    {
        if (activeVisual != null) activeVisual.SetActive(false);
        if (animatorCell == null) animatorCell = GetComponentInChildren<Animator>();
        if (trapbox == null) trapbox = GetComponentInParent<TrapBox>();
        
        // Получаем коллайдер триггера, чтобы отключать его при переноске
        myCollider = GetComponent<Collider>();
    }

    void OnTriggerEnter(Collider other)
    {
        // Проверяем доставку в парк
        if (other.CompareTag("ParkTrigger"))
        {
            if (HasCatch() && !isDelivered)
            {
                if (ParkManager.instance != null && ParkManager.instance.TryDeliverMonster())
                {
                    isDelivered = true; // Мгновенно блокируем повторный вход
                    Debug.Log("[TRAP] Monster delivered! Returning trap to inventory.");
                    
                    if (VAR_TrapsCount != null) VAR_TrapsCount.ApplyChange(1);
                    
                    Destroy(transform.parent.gameObject);
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
                    other.transform.DOMove(captureCenterPoint.position, attractionDuration);
                    other.transform.DORotateQuaternion(captureCenterPoint.rotation, attractionDuration)
                        .OnComplete(() => other.transform.SetParent(captureCenterPoint));
                }

                if (captureParticles != null) captureParticles.Play();
                if (animatorCell != null) animatorCell.SetBool("CellOpenClose", true);

                // Включаем физический коллайдер на коробке при поимке
                if (trapbox.GetComponent<BoxCollider>()) trapbox.GetComponent<BoxCollider>().enabled = true;

                caughtEnemy = other.gameObject;
                isUsed = true;
            }
        }
    }

    public bool HasCatch()
    {
        return isUsed && caughtEnemy != null;
    }

    // --- ЛОГИКА ПЕРЕНОСКИ ---

    public void AnimatePickUp(Transform holdParent)
    {
        // Переключаем ОСНОВНОЙ коллайдер коробки в режим триггера
        var boxColl = trapbox.GetComponent<Collider>();
        if (boxColl != null) boxColl.isTrigger = true;

        transform.parent.SetParent(holdParent);
        transform.parent.DOLocalMove(Vector3.zero, pickUpDuration);
        transform.parent.DOLocalRotate(Vector3.zero, pickUpDuration);
    }

    public void AnimateDrop(Vector3 targetPosition, Quaternion targetRotation)
    {
        transform.parent.SetParent(null);
        transform.parent.DOMove(targetPosition, dropDuration).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            var boxColl = trapbox.GetComponent<Collider>();
            if (boxColl != null) boxColl.isTrigger = false;
        });
        transform.parent.DORotateQuaternion(targetRotation, dropDuration);
    }
}
