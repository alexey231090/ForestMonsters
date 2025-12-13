using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAnimator : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;

    // Название параметра в Animator Controller (галочка bool)
    private const string IS_WALKING_PARAM = "IsWalking";

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        
        if(animator == null)
        print("Animator component not found");
    }

    void Update()
    {
        // Проверка: Двигается ли агент?
        // agent.velocity.sqrMagnitude > 0.1f - самый быстрый способ проверить скорость
        bool isMoving = agent.velocity.sqrMagnitude > 0.5f;

        // Передаем это в аниматор
        animator.SetBool(IS_WALKING_PARAM, isMoving);
    }
}
