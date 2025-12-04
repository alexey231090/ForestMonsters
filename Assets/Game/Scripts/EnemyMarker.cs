using UnityEngine;
//using DG.Tweening; // Подключаем DoTween

public class EnemyMarker : MonoBehaviour
{
    [Header("Animation Settings")]
    public float targetScale = 3f;       // До какого размера раздуваться
    public float duration = 1f;          // Время одного "вдоха"
    public float lifetime = 15f;         // Сколько секунд метка висит на карте перед исчезновением

    void Start()
    {
    //    // 1. Устанавливаем начальный размер
    //    transform.localScale = Vector3.one; // 1,1,1

    //    // 2. Запускаем анимацию DoTween
    //    // Изменяем Scale до targetScale за время duration
    //    // SetLoops(-1, LoopType.Yoyo) означает "повторять бесконечно туда-сюда"
    //    // SetEase(Ease.InOutSine) делает движение мягким
    //    transform.DOScale(new Vector3(targetScale, targetScale, targetScale), duration)
    //        .SetEase(Ease.InOutSine)
    //        .SetLoops(-1, LoopType.Yoyo);

    //    // 3. Удаляем объект через lifetime секунд, чтобы не засорять память
    //    Destroy(gameObject, lifetime);
    }

    void OnDestroy()
    {
        // Важно для DoTween: останавливаем анимацию при удалении, чтобы не было ошибок
        //transform.DOKill();
    }
}