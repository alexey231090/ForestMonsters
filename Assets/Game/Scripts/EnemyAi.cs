using UnityEngine;
using UnityEngine.AI;

public class EnemyAi : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2,5)] public string description = "AI врага: патрулирование и преследование цели по NavMesh; учитывает оглушение ловушкой.";
	[Header("Targeting")]
	[SerializeField] private Transform target; // за кем следить (корабль игрока)
	[SerializeField] private float activationRadius = 40f; // радиус активации слежения
	[SerializeField] private float disengageDistance = 150f; // дистанция, на которой перестаём преследовать
	[SerializeField] private float stoppingDistance = 5f; // дистанция остановки у цели
	[SerializeField] private float repathInterval = 0.25f; // период обновления пути

	[Header("Agent Settings")]
	[SerializeField] private float moveSpeed = 6f;
	[SerializeField] private float acceleration = 12f;
	[SerializeField] private float angularSpeed = 120f;

	[Header("Patrol Settings")]
	[SerializeField] private bool enablePatrol = true; // включить патрулирование
	[SerializeField] private float patrolRadius = 50f; // радиус патрулирования от начальной позиции
	[SerializeField] private float patrolWaitTime = 2f; // время ожидания на точке
	[SerializeField] private float patrolSpeed = 4f; // скорость патрулирования

	[Header("PlayMaker Integration")]
	[SerializeField] private bool isPatrolMode = false; // режим патрулирования (для PlayMaker)

	private UnityEngine.AI.NavMeshAgent agent;
	private float nextRepathTime;
	private bool isChasing; // ведём ли активное преследование сейчас
	private bool trapStunned;
	private float trapStunEndTime;
	private bool agentStoppedBeforeStun;

	// Патрулирование
	private Vector3 startPosition; // начальная позиция для патрулирования
	private Vector3 currentPatrolTarget; // текущая цель патрулирования
	private float patrolWaitTimer; // таймер ожидания на точке
	private bool isWaitingAtPoint; // ждём ли на точке

	void Awake()
	{
		agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
		if (agent == null)
		{
			Debug.LogError("[EnemyShipMovement] NavMeshAgent not found on GameObject", this);
			return;
		}

		agent.stoppingDistance = stoppingDistance;
		agent.speed = moveSpeed;
		agent.acceleration = acceleration;
		agent.angularSpeed = angularSpeed;
		agent.updateRotation = true;
		
		// Инициализация патрулирования
		startPosition = transform.position;
		GenerateNewPatrolTarget();
	}

	void Start()
	{
		// Инициализация - патрулирование запускается через PlayMaker
		StartPatrol();
	}

	void Update()
	{
		// Блокируем всю логику движения на время оглушения от ловушки
		if (trapStunned)
		{
			if (Time.time >= trapStunEndTime)
			{
				trapStunned = false;
				if (agent != null) agent.isStopped = agentStoppedBeforeStun;
			}
			else
			{
				if (agent != null) agent.isStopped = true;
				return;
			}
		}

		UpdatePatrol();
		MoveToTarget();
		

    }

	
	public void SetTarget(Transform newTarget)
	{
		target = newTarget;
	}


	public void MoveToTarget()
	{
		if (agent == null) return;

		// Если в режиме патрулирования - не преследуем
		if (isPatrolMode)
		{
			return;
		}

		// Если нет цели — стоим
		if (target == null)
		{
			if (!agent.isStopped) agent.isStopped = true;
			return;
		}

		// Проверяем дистанции и логику преследования с гистерезисом
		float distanceToTarget = Vector3.Distance(transform.position, target.position);

		// Если уже преследуем и цель слишком далеко — прекращаем преследование
		if (isChasing && distanceToTarget > disengageDistance)
		{
			
			isChasing = false;
			agent.isStopped = true;
			agent.ResetPath();
			return;
		}

		// Если ещё не преследуем — начинаем, только если цель вошла в активационный радиус
		if (!isChasing)
		{
			if (distanceToTarget > activationRadius)
			{
				if (!agent.isStopped) 
				return;
			}
			// Вошли в радиус активации — начинаем преследование
			isChasing = true;
		}

		// Активное преследование по NavMesh
		if (agent.isStopped) agent.isStopped = false;

		// Обновляем путь с заданной периодичностью
		if (Time.time >= nextRepathTime)
		{
			agent.stoppingDistance = stoppingDistance;
			agent.speed = moveSpeed;
			agent.acceleration = acceleration;
			agent.angularSpeed = angularSpeed;
			agent.SetDestination(target.position);
			nextRepathTime = Time.time + repathInterval;
		}
	}

	/// <summary>
	/// Начинает патрулирование
	/// </summary>
	public void StartPatrol()
	{
		if (!enablePatrol || agent == null) return;
		
		isChasing = false;
		agent.isStopped = false;
		agent.speed = patrolSpeed;
		agent.stoppingDistance = 1f; // маленькая дистанция остановки для патрулирования
		GenerateNewPatrolTarget();
		agent.SetDestination(currentPatrolTarget);
	}

	/// <summary>
	/// Останавливает патрулирование
	/// </summary>
	public void StopPatrol()
	{
		if (agent == null) return;
		
		agent.isStopped = true;
		agent.ResetPath();
		isWaitingAtPoint = false;
	}

	
	/// <summary>
	/// Обновляет патрулирование - вызывать в Update
	/// </summary>
	public void UpdatePatrol()
	{
		if (!enablePatrol || isChasing || agent == null)
		{
			
		 return;
		}

		// Если ждём на точке
		if (isWaitingAtPoint)
		{
			patrolWaitTimer -= Time.deltaTime;
			if (patrolWaitTimer <= 0f)
			{
				isWaitingAtPoint = false;
				StartPatrol();
				GenerateNewPatrolTarget();
				agent.SetDestination(currentPatrolTarget);
			}
			return;
		}

		// Проверяем, достигли ли цели патрулирования
		if (!agent.pathPending && agent.remainingDistance < 2f)
		{
			// Достигли точки - начинаем ждать
			isWaitingAtPoint = true;
			patrolWaitTimer = patrolWaitTime;
			agent.isStopped = true;
		}
		else if (!agent.pathPending && agent.remainingDistance < 0.5f)
		{
			// Если застряли - генерируем новую точку
			GenerateNewPatrolTarget();
			agent.SetDestination(currentPatrolTarget);
		}
	}

	/// <summary>
	/// Генерирует новую случайную точку патрулирования
	/// </summary>
	private void GenerateNewPatrolTarget()
	{
		Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
		randomDirection += startPosition;
		randomDirection.y = startPosition.y; // сохраняем высоту

		// Ищем ближайшую точку на NavMesh
		UnityEngine.AI.NavMeshHit hit;
		if (UnityEngine.AI.NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, 1))
		{
			currentPatrolTarget = hit.position;
		}
		else
		{
			// Если не нашли подходящую точку - используем случайную в радиусе
			currentPatrolTarget = startPosition + Random.insideUnitSphere * patrolRadius;
			currentPatrolTarget.y = startPosition.y;
		}
	}

	/// <summary>
	/// Возвращает true если корабль сейчас патрулирует
	/// </summary>
	public bool IsPatrolling()
	{
		return enablePatrol && !isChasing && !isWaitingAtPoint;
	}

	
	/// <summary>
	/// УНИВЕРСАЛЬНЫЙ МЕТОД ДЛЯ PLAYMAKER - патрулирование с обнаружением врага
	/// Вызывайте этот метод в Update через PlayMaker
	/// </summary>
	public void PatrolWithDetection()
	{
		if (agent == null || !enablePatrol) return;

		// Проверяем, есть ли цель и близко ли она
		if (target != null)
		{
			float distanceToTarget = Vector3.Distance(transform.position, target.position);
			
			// Если враг в зоне обнаружения - останавливаем патрулирование
			if (distanceToTarget <= activationRadius)
			{
				// Останавливаем патрулирование
				agent.isStopped = true;
				agent.ResetPath();
				isWaitingAtPoint = false;
				return; // Выходим из метода - патрулирование остановлено
			}
		}

		// Если ждём на точке
		if (isWaitingAtPoint)
		{
			patrolWaitTimer -= Time.deltaTime;
			if (patrolWaitTimer <= 0f)
			{
				isWaitingAtPoint = false;
				GenerateNewPatrolTarget();
				agent.SetDestination(currentPatrolTarget);
			}
			return;
		}

		// Проверяем, достигли ли цели патрулирования
		if (!agent.pathPending && agent.remainingDistance < 2f)
		{
			// Достигли точки - начинаем ждать
			isWaitingAtPoint = true;
			patrolWaitTimer = patrolWaitTime;
			agent.isStopped = true;
		}
		else if (!agent.pathPending && agent.remainingDistance < 0.5f)
		{
			// Если застряли - генерируем новую точку
			GenerateNewPatrolTarget();
			agent.SetDestination(currentPatrolTarget);
		}
	}

	/// <summary>
	/// Запускает патрулирование (для PlayMaker)
	/// Вызывайте один раз для начала патрулирования
	/// </summary>
	public void StartPatrolWithDetection()
	{
		if (agent == null || !enablePatrol) return;
		
		agent.isStopped = false;
		agent.speed = patrolSpeed;
		agent.stoppingDistance = 1f;
		isWaitingAtPoint = false;
		GenerateNewPatrolTarget();
		agent.SetDestination(currentPatrolTarget);
	}

	void OnDrawGizmosSelected()
	{
		// Радиус активации преследования
		Gizmos.color = new Color(1f, 0.8f, 0.1f, 0.25f);
		Gizmos.DrawSphere(transform.position, activationRadius);
		Gizmos.color = new Color(1f, 0.8f, 0.1f, 1f);
		Gizmos.DrawWireSphere(transform.position, activationRadius);

		// Дистанция прекращения преследования
		Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.12f);
		Gizmos.DrawSphere(transform.position, disengageDistance);
		Gizmos.color = new Color(0.2f, 0.6f, 1f, 1f);
		Gizmos.DrawWireSphere(transform.position, disengageDistance);

		// Визуализация патрулирования
		if (enablePatrol)
		{
			// Радиус патрулирования
			Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
			Gizmos.DrawSphere(startPosition, patrolRadius);
			Gizmos.color = new Color(0f, 1f, 0f, 1f);
			Gizmos.DrawWireSphere(startPosition, patrolRadius);

			// Текущая цель патрулирования
			if (currentPatrolTarget != Vector3.zero)
			{
				Gizmos.color = Color.green;
				Gizmos.DrawWireCube(currentPatrolTarget, Vector3.one * 2f);
				Gizmos.DrawLine(transform.position, currentPatrolTarget);
			}

			// Начальная позиция
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(startPosition, Vector3.one * 3f);
		}
	}

	public void StunByTrap(float duration)
	{
		if (agent == null) return;

		trapStunned = true;
		trapStunEndTime = Time.time + duration;
		agentStoppedBeforeStun = agent.isStopped;

		agent.isStopped = true;
		agent.ResetPath();
	}
}
