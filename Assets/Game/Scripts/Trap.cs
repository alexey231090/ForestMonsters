using UnityEngine;
using UnityEngine.AI;

public class Trap : MonoBehaviour
{
    [Header("Description")]
    [TextArea(2,5)] public string description = "Ловушка: оглушает врага, позволяет собрать добычу по E и затем уничтожается.";
    private bool isUsed = false;
    public float trapStunDuration = 10f;

    // Ссылка на пойманного врага
    private GameObject caughtEnemy;

    // Сюда в инспекторе можно перетащить какой-то индикатор (лампочку), который загорится
    public GameObject activeVisual;

    void Start()
    {
        if (activeVisual != null) activeVisual.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isUsed) return;

        if (other.CompareTag("Enemy"))
        {
            Debug.Log("Враг попался!");

            var enemy = other.GetComponent<EnemyAi>();
            if (enemy != null)
            {
                enemy.StunByTrap(trapStunDuration);

                // Запоминаем врага и блокируем ловушку
                caughtEnemy = other.gameObject;
                isUsed = true;

                // Визуальный эффект (покраснение)
                GetComponent<Renderer>().material.color = Color.red;
                if (activeVisual != null) activeVisual.SetActive(true);
            }
        }
    }

    // Этот метод проверит, есть ли добыча
    public bool HasCatch()
    {
        return isUsed && caughtEnemy != null;
    }

    // Этот метод вызовет игрок, когда нажмет E
    public void CollectPrey()
    {
        if (caughtEnemy != null)
        {
            Destroy(caughtEnemy); // Удаляем врага со сцены
            Debug.Log("ДОБЫЧА СОБРАНА! (+1 очко)");
            // Тут позже добавим ScoreManager.instance.AddScore(1);
        }

        // Уничтожаем саму ловушку (одноразовая)
        Destroy(gameObject);
    }
}


