using UnityEngine;
//using DG.Tweening; // ���������� DoTween

public class EnemyMarker : MonoBehaviour
{
    [Header("Animation Settings")]
    public float targetScale = 3f;       // �� ������ ������� �����������
    public float duration = 1f;          // ����� ������ "�����"
    public float lifetime = 15f;         // ������� ������ ����� ����� �� ����� ����� �������������

    void Start()
    {
    //    // 1. ������������� ��������� ������
    //    transform.localScale = Vector3.one; // 1,1,1

    //    // 2. ��������� �������� DoTween
    //    // �������� Scale �� targetScale �� ����� duration
    //    // SetLoops(-1, LoopType.Yoyo) �������� "��������� ���������� ����-����"
    //    // SetEase(Ease.InOutSine) ������ �������� ������
    //    transform.DOScale(new Vector3(targetScale, targetScale, targetScale), duration)
    //        .SetEase(Ease.InOutSine)
    //        .SetLoops(-1, LoopType.Yoyo)

    //    // 3. ������� ������ ����� lifetime ������, ����� �� �������� ������
    //    Destroy(gameObject, lifetime);
    }

    void OnDestroy()
    {
        // ����� ��� DoTween: ������������� �������� ��� ��������, ����� �� ���� ������
        //transform.DOKill();
    }
}