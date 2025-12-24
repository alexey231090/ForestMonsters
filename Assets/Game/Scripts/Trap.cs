using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class Trap : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2,5)] public string description = "Ловушка: оглушает врага, притягивает в центр, проигрывает анимацию и партиклы.";

    [Header("Settings")]
    private bool isUsed = false;
    public float attractionDuration = 1f;

    [Header("References")]
    public Animator animatorCell; 
    public Transform captureCenterPoint; 
    public ParticleSystem captureParticles; 
    public GameObject activeVisual; 

    private GameObject caughtEnemy;
    private TrapBox trapbox;

    void Start()
    {
        if (activeVisual != null) activeVisual.SetActive(false);
        if (animatorCell == null) animatorCell = GetComponentInChildren<Animator>();
        if (trapbox == null) trapbox = GetComponentInParent<TrapBox>();
        
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
                // 1. ВЫКЛЮЧАЕМ МОЗГИ ВРАГА (Это исправит ошибку!)
                // Чтобы он перестал обращаться к NavMeshAgent
                enemyAI.enabled = false; 

                // 2. Отключаем NavMeshAgent
                var agent = other.GetComponent<NavMeshAgent>();
                if (agent != null) agent.enabled = false;

                // 3. Отключаем физику (Rigidbody)
                var rb = other.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;

                // 4. Притягиваем к центру
                if (captureCenterPoint != null)
                {
                    other.transform.DOMove(captureCenterPoint.position, attractionDuration);
                    other.transform.DORotateQuaternion(captureCenterPoint.rotation, attractionDuration).OnComplete(() =>
                    {
                        other.transform.SetParent(captureCenterPoint);
                    });
                }

                // 5. Партиклы
                if (captureParticles != null) captureParticles.Play();

                // 6. Анимация клетки
                if (animatorCell != null) animatorCell.SetBool("CellOpenClose", true);

                //7.Включаем коллайдер на клетке
                trapbox.GetComponent<BoxCollider>().enabled = true;

                // Финализация
                caughtEnemy = other.gameObject;
                isUsed = true;
            }
        }
    }

    public bool HasCatch()
    {
        return isUsed && caughtEnemy != null;
    }

    public void CollectPrey()
    {
        if (caughtEnemy != null)
        {
            Destroy(caughtEnemy); 
            if (GameManager.instance != null) GameManager.instance.AddCreature();
        }
        
        
        
        Destroy(trapbox.gameObject);
    }
}