using UnityEngine;
using System.Collections;

public class VisitorSpawner : MonoBehaviour
{
    [Header("Settings")]
    public int minVisitors = 1;
    public int maxVisitors = 15;

    // Ссылка на корутину, чтобы можно было остановить её ночью
    private Coroutine dailyRoutine;

    public void StartNewDay()
    {
        // Останавливаем предыдущие процессы, если были
        StopSpawning();

        // Запускаем новый день
        dailyRoutine = StartCoroutine(VirtualVisitorRoutine());
    }

    public void StopSpawning()
    {
        if (dailyRoutine != null)
        {
            StopCoroutine(dailyRoutine);
            dailyRoutine = null;
        }
    }

    IEnumerator VirtualVisitorRoutine()
    {
        // 1. Решаем, сколько людей придет сегодня
        int visitorsCount = Random.Range(minVisitors, maxVisitors + 1);
        Debug.Log($"[Прогноз] Сегодня парк планируют посетить {visitorsCount} человек.");

        for (int i = 0; i < visitorsCount; i++)
        {
            // Случайная задержка между посетителями (от 2 до 5 секунд)
            yield return new WaitForSeconds(Random.Range(2f, 5f));

            // 2. ПРОВЕРКА: Есть ли мемы в парке?
            int memesCount = GameManager.instance.activePlatforms.Count;

            if (memesCount > 0)
            {
                // Расчет цены: кол-во мемов * цену
                float payAmount = memesCount * GameManager.instance.pricePerMeme;

                Debug.Log($"[Посетитель #{i + 1}] Осмотрел {memesCount} мемов.");
                GameManager.instance.AddMoney(payAmount);
            }
            else
            {
                // Если мемов нет - посетитель не приходит или уходит молча
                // Можно раскомментировать строку ниже, если хочешь видеть жалобы
                // Debug.Log($"[Посетитель #{i+1}] Развернулся у входа: «В парке пусто!»");
            }
        }
    }
}
