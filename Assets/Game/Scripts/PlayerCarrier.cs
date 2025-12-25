using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
    
    public class PlayerCarrier : MonoBehaviour 
    {
        [Header("Carry Settings")]
        public Transform holdPoint;
        public float holdTimeRequired = 1.0f;
        public float dropEmbedDepth = 0.2f; // Насколько утапливать при сбросе
        public LayerMask groundLayer;
    
        [Header("UI")]
        public Image holdProgressBar;
    
        // Внутренние переменные
        private Trap carriedTrap; // Объект, который мы сейчас несем
        private float currentHoldTimer = 0f;
    
        void Start()
        {
            if (holdProgressBar) holdProgressBar.fillAmount = 0;
        }
    
        void Update()
        {
            // Если мы что-то несем, ждем нажатия E для сброса
            if (carriedTrap != null)
            {
                if (holdProgressBar) holdProgressBar.fillAmount = 0; // Скрываем круг
                
                if (Input.GetKeyDown(KeyCode.E))
                {
                    TryDrop();
                }
            }
            else
            {
                // Если ничего не несем и кнопка E не нажата - сбрасываем таймер
                if (!Input.GetKey(KeyCode.E))
                {
                    ResetHoldTimer();
                }
            }
        }
    
        public bool IsCarrying()
        {
            return carriedTrap != null;
        }
    
        // Вызывается из PlayerInteract каждый кадр, пока мы смотрим на объект и держим E
        public void ProcessHold(GameObject targetObj)
        {
             if (IsCarrying()) return; // Уже заняты руки
    
            // Увеличиваем таймер
            currentHoldTimer += Time.deltaTime;
            
            // Обновляем UI
            if (holdProgressBar)
                holdProgressBar.fillAmount = currentHoldTimer / holdTimeRequired;
    
            // Если удержали нужное время
            if (currentHoldTimer >= holdTimeRequired)
            {
                PerformPickup(targetObj);
                ResetHoldTimer();
            }
        }
    
        public void ResetHoldTimer()
        {
            currentHoldTimer = 0f;
            if (holdProgressBar) holdProgressBar.fillAmount = 0;
        }
    
        // Логика: Что делать с объектом (Взять в руки или В инвентарь)
        void PerformPickup(GameObject obj)
        {
            // 1. Проверяем, это ЛОВУШКА?
            // Ищем компонент Trap или TrapBox
            Trap trap = obj.GetComponent<Trap>();
            if (trap == null) trap = obj.GetComponentInChildren<Trap>();
    
            if (trap != null)
            {
                if (trap.HasCatch())
                {
                    // Если есть добыча -> БЕРЕМ В РУКИ
                    PickUpPhysical(trap);
                }
                else
                {
                    // Если пустая -> ВОЗВРАЩАЕМ В ИНВЕНТАРЬ
                    if (GameManager.instance != null) GameManager.instance.trapsCount++;
                    
                    // Удаляем весь объект (включая родителя TrapBox)
                    if (trap.trapbox != null) Destroy(trap.trapbox.gameObject);
                    else Destroy(trap.gameObject);
                    
                    Debug.Log("Пустая ловушка возвращена в инвентарь.");
                }
                return;
            }
    
            // 2. Проверяем, это КАМЕРА? (Предполагаем, что у камеры есть скрипт SecurityCameraSetup или тег)
            // Для простоты проверим по компоненту Camera или SecurityCameraSetup
            if (obj.GetComponentInParent<SecurityCameraSetup>() != null)
            {
                if (GameManager.instance != null) GameManager.instance.camerasCount++;
                Destroy(obj); // Удаляем со сцены
                Debug.Log("Камера возвращена в инвентарь.");
                return;
            }
        }
    
        // --- ФИЗИЧЕСКАЯ ПЕРЕНОСКА (Только для ловушек с добычей) ---
    
        void PickUpPhysical(Trap trap)
        {
            carriedTrap = trap;
            
    
            Collider[] cols = trap.GetComponentsInChildren<Collider>();
            foreach (var c in cols) c.enabled = false;
    
            // Привязываем к рукам
            Transform targetTransform = trap.trapbox != null ? trap.trapbox.transform : trap.transform;
            targetTransform.SetParent(holdPoint);
    
            // Анимация полета в руки
            targetTransform.DOLocalMove(Vector3.zero, 0.5f);
            targetTransform.DOLocalRotate(Vector3.zero, 0.5f);
    
            Debug.Log("Клетка с монстром взята!");
        }
    
        void TryDrop()
        {
            RaycastHit hit;
            // Пускаем луч вниз от рук
            if (Physics.Raycast(holdPoint.position, Vector3.down, out hit, 10f, groundLayer))
            {
                DropPhysical(hit.point);
            }
            else
            {
                Debug.Log("Нет земли, чтобы поставить!");
            }
        }
    
        void DropPhysical(Vector3 floorPos)
        {
            // Расчет позиции с утоплением
            Vector3 finalPos = floorPos - new Vector3(0, dropEmbedDepth, 0);
    
            // Локальная ссылка перед обнулением
            Trap trapToDrop = carriedTrap;
            Transform targetTransform = trapToDrop.trapbox != null ? trapToDrop.trapbox.transform : trapToDrop.transform;
    
            targetTransform.SetParent(null);
    
            // Анимация падения
            targetTransform.DOMove(finalPos, 0.5f).SetEase(Ease.OutBounce).OnComplete(() =>
            {
                if (trapToDrop != null)
                {
                    Collider[] cols = trapToDrop.GetComponentsInChildren<Collider>();
                    foreach (var c in cols) c.enabled = true;
    
                    // Физику можно не включать, если хочешь, чтобы стояла намертво
                    // var rb = targetTransform.GetComponent<Rigidbody>();
                    // if (rb) rb.isKinematic = false; 
                }
            });
    
            // Поворот по Y игрока
            Quaternion targetRot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            targetTransform.DORotateQuaternion(targetRot, 0.5f);
    
            carriedTrap = null; // Руки свободны
            Debug.Log("Клетка поставлена!");
        }
    }

