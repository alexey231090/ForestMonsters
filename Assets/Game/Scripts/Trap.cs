using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class Trap : MonoBehaviour
{
    [Header("Settings")]
    public float attractionDuration = 1f;
    public float pickUpDuration = 0.5f; // Скорость подъема к рукам
    public float dropDuration = 0.5f;   // Скорость опускания на землю

    [Header("References")]
    public Animator animatorCell;
    public Transform captureCenterPoint;
    public ParticleSystem captureParticles;
    public GameObject activeVisual;
    public TrapBox trapbox; // Ссылка на родительский объект (сама коробка)

    private bool isUsed = false;
    private GameObject caughtEnemy;
    private Collider myCollider; // Коллайдер самой ловушки/триггера

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
        if (isUsed) return;

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Враг попался");
            var enemyAI = other.GetComponent<EnemyAi>();
            if (enemyAI != null)
            {
                enemyAI.IsCaught = true;
                enemyAI.enabled = false; // Выключаем мозг
                var agent = other.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                // Притягиваем
                if (captureCenterPoint != null)
                {
                    other.transform.DOMove(captureCenterPoint.position, attractionDuration);
                    other.transform.DORotateQuaternion(captureCenterPoint.rotation, attractionDuration)
                        .OnComplete(() => other.transform.SetParent(captureCenterPoint));
                }

                if (captureParticles != null) captureParticles.Play();
                if (animatorCell != null) animatorCell.SetBool("CellOpenClose", true);

                // Включаем физический коллайдер на коробке (если он был выключен)
                if(trapbox.GetComponent<BoxCollider>()) trapbox.GetComponent<BoxCollider>().enabled = true;

                caughtEnemy = other.gameObject;
                isUsed = true;
            }
        }
        else if (other.CompareTag("ParkTrigger"))
        {
            if (HasCatch())
            {
                if (ParkManager.instance != null && ParkManager.instance.TryDeliverMonster())
                {
                    Debug.Log("[TRAP] Monster delivered! Returning trap to inventory.");
                    
                    if (GameManager.instance != null)
                    {
                        GameManager.instance.trapsCount++;
                    }
                    
                    // Удаляем всего родителя (TrapBox), так как Trap висит на нем
                    Destroy(transform.parent.gameObject);
                }
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
        // Отключаем физику и коллайдеры, чтобы не толкать игрока
        if (myCollider) myCollider.enabled = false;
        if (trapbox.GetComponent<Collider>()) trapbox.GetComponent<Collider>().enabled = false;

        // Перемещаем к рукам
        transform.parent.SetParent(holdParent); // Берем всего родителя (TrapBox)
        
        // Анимация полета в руки
        transform.parent.DOLocalMove(Vector3.zero, pickUpDuration);
        transform.parent.DOLocalRotate(Vector3.zero, pickUpDuration);
    }

    public void AnimateDrop(Vector3 targetPosition, Quaternion targetRotation)
    {
        // Отцепляем от игрока
        transform.parent.SetParent(null);

        // Анимация падения на землю
        transform.parent.DOMove(targetPosition, dropDuration).SetEase(Ease.OutBounce).OnComplete(() =>
        {
            // Когда упала - включаем коллайдеры обратно (чтобы можно было снова взять)
            if (myCollider) myCollider.enabled = true;
            if (trapbox.GetComponent<Collider>()) trapbox.GetComponent<Collider>().enabled = true;
        });

        transform.parent.DORotateQuaternion(targetRotation, dropDuration);
    }
}
