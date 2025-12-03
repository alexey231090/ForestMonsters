using UnityEngine;

public class ParkPlatform : MonoBehaviour
{
    [Header("Visuals")]
    public GameObject monsterModel; // Ссылка на модельку монстра, которая будет включаться

    [Header("Settings")]
    public bool isOccupied = false; // Занята ли платформа
    public int incomePerVisitor = 5; // Сколько денег приносит (ценность)

    void Start()
    {
        // При старте игры прячем монстра, так как платформа пустая
        if (monsterModel != null)
        {
            monsterModel.SetActive(false);
        }
        isOccupied = false;
    }

    // Этот метод вызовет PlayerInteract, когда нажмем E
    public void TryPlaceMonster()
    {
        // 1. Проверяем, не занято ли уже
        if (isOccupied)
        {
            Debug.Log("Здесь уже сидит монстр!");
            return;
        }

        // 2. Проверяем, есть ли монстры в инвентаре (через GameManager)
        // GameManager сам уменьшит счетчик capturedCreatures внутри метода TryRemoveCreature
        if (GameManager.instance != null && GameManager.instance.TryRemoveCreature())
        {
            // Успех! Ставим монстра
            isOccupied = true;

            if (monsterModel != null)
            {
                monsterModel.SetActive(true);
            }

            // Добавляем эту платформу в список активных, чтобы посетители её видели
            GameManager.instance.activePlatforms.Add(this);

            Debug.Log("Монстр размещен на платформе!");
        }
        else
        {
            Debug.Log("У вас в мешке нет монстров! Поймайте их ночью.");
        }
    }
}