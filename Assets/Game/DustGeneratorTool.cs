using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DustGeneratorTool : MonoBehaviour
{
    // Этот код будет работать только в редакторе Unity
#if UNITY_EDITOR
    [MenuItem("Tools/VFX/Generate Ground Dust")]
    public static void CreateDustEffect()
    {
        // 1. Создаем объект
        GameObject go = new GameObject("VFX_GroundDust_Burst");
        
        // Сразу ставим перед камерой сцены, чтобы ты его увидел
        if (SceneView.lastActiveSceneView != null)
        {
            go.transform.position = SceneView.lastActiveSceneView.pivot;
            go.transform.position += Vector3.down * 2f; // Чуть опускаем
        }
        else
        {
            go.transform.position = Vector3.zero;
        }

        // Поворачиваем, чтобы пыль летела параллельно земле
        go.transform.rotation = Quaternion.Euler(-90, 0, 0);

        // 2. Навешиваем компоненты
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();

        // --- MAIN MODULE ---
        var main = ps.main;
        main.duration = 1.0f;
        main.loop = false;               // Это же ваншот
        main.startLifetime = 1.5f;       // Время жизни
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 4f); // Разброс скорости
        main.startSize = new ParticleSystem.MinMaxCurve(0.4f, 1.2f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0, 360f);
        main.gravityModifier = 0.1f;     // Чуть-чуть гравитации, чтобы оседала
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;
        main.stopAction = ParticleSystemStopAction.Destroy; // Самоуничтожение (удобно для префаба)
        
        // Цвет (пыльно-серый)
        main.startColor = new Color(0.7f, 0.65f, 0.6f, 0.5f);

        // --- EMISSION ---
        var emission = ps.emission;
        emission.rateOverTime = 0;
        // Взрывной выброс (Burst)
        emission.SetBursts(new ParticleSystem.Burst[] { 
            new ParticleSystem.Burst(0.0f, (short)Random.Range(20, 30)) 
        });

        // --- SHAPE (Самое важное для "распластывания") ---
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        shape.radiusThickness = 0f; // Из центра
        // Делаем круг плоским
        shape.rotation = new Vector3(0, 0, 0); 
        // Важно: в скрипте мы повернули сам GO на -90, так что Circle будет лежать на земле.

        // --- VELOCITY OVER LIFETIME (Чтобы "растекалась" и тормозила) ---
        var vel = ps.limitVelocityOverLifetime;
        vel.enabled = true;
        vel.dampen = 0.25f; // Сильное трение воздуха
        vel.limit = 0f;

        // --- SIZE OVER LIFETIME (Пыль расширяется) ---
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0.0f, 0.2f); // Появляется маленькой
        sizeCurve.AddKey(0.2f, 1.0f); // Резко вырастает
        sizeCurve.AddKey(1.0f, 1.5f); // Медленно растет в конце
        sol.size = new ParticleSystem.MinMaxCurve(1.0f, sizeCurve);

        // --- COLOR OVER LIFETIME (Растворение) ---
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.grey, 1.0f) },
            new GradientAlphaKey[] { 
                new GradientAlphaKey(0.0f, 0.0f), 
                new GradientAlphaKey(0.8f, 0.1f), // Быстрое появление
                new GradientAlphaKey(0.0f, 1.0f)  // Плавное исчезновение
            }
        );
        col.color = grad;

        // --- RENDERER ---
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        
        // Пытаемся найти встроенный материал "Default-Particle" (мягкий круг)
        // В разных версиях Unity путь может отличаться, это универсальный хак для Editor-скрипта:
        Material defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Particle.mat");
        
        // Если вдруг не нашли (редкость), берем Sprites-Default (он тоже может быть квадратом, но прозрачным)
        if (defaultMat == null) 
            defaultMat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");

        psr.material = defaultMat;
        psr.sortMode = ParticleSystemSortMode.Distance;
    }
#endif
}
