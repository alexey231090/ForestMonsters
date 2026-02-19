using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(GameEvent))]
public class GameEventEditor : Editor
{
    private List<GameObject> _prefabListeners = new List<GameObject>();
    private List<GameEventListener> _sceneListeners = new List<GameEventListener>();
    private List<MonoBehaviour> _sceneSignalBinders = new List<MonoBehaviour>();

    // Стили
    private GUIStyle _boxStyle;
    private GUIStyle _methodStyle;
    private GUIStyle _signalStyle;

    private void OnEnable()
    {
        FindProjectReferences();
        FindSceneReferences();
    }

    private void InitStyles()
    {
        if (_boxStyle == null)
        {
            _boxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(4, 4, 2, 2)
            };
        }

        if (_methodStyle == null)
        {
            _methodStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                wordWrap = true
            };
        }

        if (_signalStyle == null)
        {
            _signalStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                wordWrap = true
            };
        }
    }

    public override void OnInspectorGUI()
    {
        InitStyles();

        // 1. Рисуем стандартный инспектор (поля ScriptableObject)
        DrawDefaultInspector();

        GameEvent gameEvent = (GameEvent)target;

        EditorGUILayout.Space(10);

        // 2. Кнопка для теста события (активна только в Play Mode)
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("⚡ Raise Event"))
        {
            gameEvent.Raise();
            Debug.Log($"<color=green>Event {gameEvent.name} Raised from Editor!</color>");
        }
        GUI.enabled = true;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("📊 DEPENDENCIES INFO", EditorStyles.boldLabel);

        // ─── 3. Старая система: GameEventListener на сцене ───
        EditorGUILayout.LabelField($"🎬 Classic Listeners (Scene): {_sceneListeners.Count}", EditorStyles.boldLabel);
        if (_sceneListeners.Count > 0)
        {
            foreach (var listener in _sceneListeners)
            {
                if (listener != null)
                    DrawListenerCard(listener);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No classic listeners in current scene.", MessageType.Info);
        }

        EditorGUILayout.Space(10);

        // ─── 4. Новая система: SignalBinder на сцене ───
        EditorGUILayout.LabelField($"🧠 Signal Binders (Scene): {_sceneSignalBinders.Count}", EditorStyles.boldLabel);
        if (_sceneSignalBinders.Count > 0)
        {
            foreach (var smart in _sceneSignalBinders)
            {
                if (smart != null)
                    DrawSmartListenerCard(smart);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No SignalBinder subscribers in current scene.", MessageType.Info);
        }

        if (GUILayout.Button("🔄 Refresh Scene References"))
        {
            FindSceneReferences();
        }

        EditorGUILayout.Space(10);

        // 5. Ссылки в ПРОЕКТЕ (Префабы) с методами
        EditorGUILayout.LabelField($"📦 Prefab References (Project): {_prefabListeners.Count}", EditorStyles.boldLabel);
        if (_prefabListeners.Count > 0)
        {
            foreach (var prefab in _prefabListeners)
            {
                DrawPrefabCard(prefab);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No prefab references found.", MessageType.None);
        }

        if (GUILayout.Button("🔄 Refresh Project References"))
        {
            FindProjectReferences();
        }
    }

    // ───────── Карточка классического слушателя на сцене ─────────
    private void DrawListenerCard(GameEventListener listener)
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        EditorGUILayout.ObjectField(listener.gameObject, typeof(GameObject), true);
        DrawUnityEventMethods(listener.Response, "  → ");

        EditorGUILayout.EndVertical();
    }

    // ───────── Карточка SignalBinder на сцене ─────────
    private void DrawSmartListenerCard(MonoBehaviour smart)
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        EditorGUILayout.ObjectField(smart.gameObject, typeof(GameObject), true);

        string typeName = smart.GetType().Name;
        string label = $"  🧠 <b>{smart.gameObject.name}</b>  <color=#4EC9B0>{typeName}</color>.<color=#DCDCAA>OnSignalReceived</color>()";
        EditorGUILayout.LabelField(label, _signalStyle);

        // Попробуем показать привязанные методы через рефлексию словаря _eventMap
        var fieldInfo = typeof(SignalBinder).GetField("_eventMap", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo != null)
        {
            var eventMap = fieldInfo.GetValue(smart) as System.Collections.IDictionary;
            if (eventMap != null)
            {
                GameEvent targetEvent = (GameEvent)target;
                foreach (System.Collections.DictionaryEntry entry in eventMap)
                {
                    GameEvent ev = entry.Key as GameEvent;
                    if (ev == targetEvent)
                    {
                        var action = entry.Value as System.Action;
                        if (action != null)
                        {
                            string methodLabel = $"    → <color=#DCDCAA>{action.Method.Name}</color>()";
                            EditorGUILayout.LabelField(methodLabel, _signalStyle);
                        }
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    // ───────── Карточка префаба из проекта ─────────
    private void DrawPrefabCard(GameObject prefab)
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);

        GameEvent targetEvent = (GameEvent)target;

        // Старая система на префабе
        GameEventListener[] listeners = prefab.GetComponentsInChildren<GameEventListener>(true);
        foreach (var listener in listeners)
        {
            if (listener.Event == targetEvent)
                DrawUnityEventMethods(listener.Response, "  → ");
        }

        // Новая система на префабе — показываем тип SignalBinder
        SignalBinder[] smarts = prefab.GetComponentsInChildren<SignalBinder>(true);
        foreach (var smart in smarts)
        {
            string typeName = smart.GetType().Name;
            string label = $"  🧠 <color=#4EC9B0>{typeName}</color> (SignalBinder)";
            EditorGUILayout.LabelField(label, _signalStyle);
        }

        EditorGUILayout.EndVertical();
    }

    // ───────── Извлечение методов из UnityEvent ─────────
    private void DrawUnityEventMethods(UnityEvent unityEvent, string prefix)
    {
        if (unityEvent == null) return;

        int count = unityEvent.GetPersistentEventCount();

        if (count == 0)
        {
            EditorGUILayout.LabelField($"{prefix}<color=#888>(no methods)</color>", _methodStyle);
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Object eventTarget = unityEvent.GetPersistentTarget(i);
            string methodName = unityEvent.GetPersistentMethodName(i);

            string targetName = eventTarget != null ? eventTarget.GetType().Name : "null";
            string objectName = eventTarget != null ? eventTarget.name : "???";

            string label = $"{prefix}<b>{objectName}</b>  <color=#4EC9B0>{targetName}</color>.<color=#DCDCAA>{methodName}</color>()";
            EditorGUILayout.LabelField(label, _methodStyle);
        }
    }

    // ───────── Поиск на сцене ─────────
    private void FindSceneReferences()
    {
        _sceneListeners.Clear();
        _sceneSignalBinders.Clear();

        GameEvent targetEvent = (GameEvent)target;

        // Старая система
        GameEventListener[] allListeners = FindObjectsOfType<GameEventListener>(true);
        foreach (var listener in allListeners)
        {
            if (listener.Event == targetEvent)
                _sceneListeners.Add(listener);
        }

        // Новая система — ищем все SignalBinder и проверяем их поля
        SignalBinder[] allSmarts = FindObjectsOfType<SignalBinder>(true);
        foreach (var smart in allSmarts)
        {
            // Ищем публичные поля типа GameEvent через рефлексию
            var fields = smart.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(GameEvent))
                {
                    GameEvent ev = field.GetValue(smart) as GameEvent;
                    if (ev == targetEvent)
                    {
                        _sceneSignalBinders.Add(smart);
                        break; // Не добавлять один SignalBinder дважды
                    }
                }
            }
        }
    }

    // ───────── Поиск в проекте ─────────
    private void FindProjectReferences()
    {
        _prefabListeners.Clear();
        string targetPath = AssetDatabase.GetAssetPath(target);

        string[] guids = AssetDatabase.FindAssets("t:Prefab");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string[] dependencies = AssetDatabase.GetDependencies(path, false);

            bool isDependent = false;
            foreach (var dep in dependencies)
            {
                if (dep == targetPath)
                {
                    isDependent = true;
                    break;
                }
            }

            if (isDependent)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                _prefabListeners.Add(go);
            }
        }
    }
}