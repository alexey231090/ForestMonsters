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
    
    // Separated by purpose
    private List<MonoBehaviour> _sceneSubscribers = new List<MonoBehaviour>(); // Listeners (GET_)
    private List<MonoBehaviour> _sceneInvokers = new List<MonoBehaviour>();    // Callers (call_)

    // Styles
    private GUIStyle _boxStyle;
    private GUIStyle _methodStyle;
    private GUIStyle _signalStyle;
    private GUIStyle _headerStyle;

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
                wordWrap = true,
                fontSize = 12
            };
        }

        if (_signalStyle == null)
        {
            _signalStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                wordWrap = true,
                fontSize = 13
            };
        }

        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 15,
                richText = true
            };
        }
    }

    public override void OnInspectorGUI()
    {
        InitStyles();

        // 1. Draw the standard inspector (ScriptableObject fields)
        DrawDefaultInspector();

        GameEvent gameEvent = (GameEvent)target;

        EditorGUILayout.Space(10);

        // 2. Test event button (only active in Play Mode)
        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("⚡ Raise Event"))
        {
            gameEvent.Raise();
            Debug.Log($"<color=green>Event {gameEvent.name} Raised from Editor!</color>");
        }
        GUI.enabled = true;

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("📊 DEPENDENCIES INFO", EditorStyles.boldLabel);

        // ─── Variant B: GameEventListener on scene ───
        EditorGUILayout.LabelField($"🎬 <color=#2196F3>EVENT LISTENERS</color> (Variant B): {_sceneListeners.Count}", _headerStyle);
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
            EditorGUILayout.HelpBox("No GameEventListeners in current scene.", MessageType.None);
        }

        EditorGUILayout.Space(10);

        // ─── Subscribers (Listeners) ───
        EditorGUILayout.LabelField($"📥 <color=#4CAF50>SUBSCRIBERS</color> (Listen for this): {_sceneSubscribers.Count}", _headerStyle);
        if (_sceneSubscribers.Count > 0)
        {
            foreach (var smart in _sceneSubscribers)
            {
                if (smart != null) DrawSmartListenerCard(smart, true);
            }
        }
        else EditorGUILayout.HelpBox("No scripts are listening for this event.", MessageType.None);

        EditorGUILayout.Space(10);

        // ─── Invokers (Raisers) ───
        EditorGUILayout.LabelField($"📤 <color=#FF9800>INVOKERS</color> (Raise this): {_sceneInvokers.Count}", _headerStyle);
        if (_sceneInvokers.Count > 0)
        {
            foreach (var smart in _sceneInvokers)
            {
                if (smart != null) DrawSmartListenerCard(smart, false);
            }
        }
        else EditorGUILayout.HelpBox("No scripts are raising this event.", MessageType.None);

        if (GUILayout.Button("🔄 Refresh Scene References"))
        {
            FindSceneReferences();
        }

        EditorGUILayout.Space(10);

        // 5. References in PROJECT (Prefabs) with methods
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

    private void DrawListenerCard(GameEventListener listener)
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        EditorGUILayout.ObjectField(listener.gameObject, typeof(GameObject), true);
        
        string label = $"  🔵 <b>{listener.gameObject.name}</b>  <color=#4EC9B0>GameEventListener</color>.<color=#DCDCAA>Response</color>()";
        EditorGUILayout.LabelField(label, _signalStyle);
        
        DrawUnityEventMethods(listener.Response, "    → ");

        EditorGUILayout.EndVertical();
    }

    // ───────── SignalBinder card on scene ─────────
    private void DrawSmartListenerCard(MonoBehaviour smart, bool showBindings)
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        EditorGUILayout.ObjectField(smart.gameObject, typeof(GameObject), true);

        string typeName = smart.GetType().Name;
        string label = $"  🧠 <b>{smart.gameObject.name}</b>  <color=#4EC9B0>{typeName}</color>.<color=#DCDCAA>OnSignalReceived</color>()";
        EditorGUILayout.LabelField(label, _signalStyle);

        if (showBindings && smart is SignalBinder)
        {
            // Try to show bound methods via reflection of _eventMap dictionary
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
        }

        EditorGUILayout.EndVertical();
    }

    // ───────── Prefab card from project ─────────
    private void DrawPrefabCard(GameObject prefab)
    {
        EditorGUILayout.BeginVertical(_boxStyle);

        EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);

        GameEvent targetEvent = (GameEvent)target;

        // Variant B on prefab
        GameEventListener[] listeners = prefab.GetComponentsInChildren<GameEventListener>(true);
        foreach (var listener in listeners)
        {
            if (listener.Event == targetEvent)
                DrawUnityEventMethods(listener.Response, "  → ");
        }

        // Variant A on prefab — show SignalBinder type
        SignalBinder[] smarts = prefab.GetComponentsInChildren<SignalBinder>(true);
        foreach (var smart in smarts)
        {
            string typeName = smart.GetType().Name;
            string label = $"  🧠 <color=#4EC9B0>{typeName}</color> (SignalBinder)";
            EditorGUILayout.LabelField(label, _signalStyle);
        }

        EditorGUILayout.EndVertical();
    }

    // ───────── Extract methods from UnityEvent ─────────
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

    // ───────── Find on scene ─────────
    private void FindSceneReferences()
    {
        _sceneListeners.Clear();
        _sceneSubscribers.Clear();
        _sceneInvokers.Clear();

        GameEvent targetEvent = (GameEvent)target;

        // Variant B
        GameEventListener[] allListeners = FindObjectsByType<GameEventListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var listener in allListeners)
        {
            if (listener != null && listener.Event == targetEvent)
                _sceneListeners.Add(listener);
        }

        // Variant A — find all MonoBehaviours to check their fields
        // Using MonoBehaviour instead of SignalBinder to find ANY script that references the event
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script == null || script is GameEventListener) continue;
            
            System.Type type = script.GetType();
            bool isSubscriber = false;
            bool isInvoker = false;

            while (type != null && type != typeof(MonoBehaviour))
            {
                var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (field.FieldType == typeof(GameEvent))
                    {
                        GameEvent ev = field.GetValue(script) as GameEvent;
                        if (ev == targetEvent)
                        {
                            isSubscriber = true; // By default, found reference is at least an invoker

                            // 1. ПРОВЕРЯЕМ АТРИБУТЫ [Listen] (Самый точный поиск)
                            var methodWithListen = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            foreach(var m in methodWithListen)
                            {
                                var listens = m.GetCustomAttributes<ListenAttribute>();
                                foreach(var l in listens)
                                {
                                    if (l.EventFieldName == field.Name)
                                    {
                                        isSubscriber = true;
                                        break;
                                    }
                                }
                            }

                            // 2. Ищем явные префиксы подписчиков
                            string fieldName = field.Name.ToLower();
                            bool inheritSignalBinder = script is SignalBinder;

                            if (fieldName.StartsWith("get_") || fieldName.StartsWith("sub_") || fieldName.StartsWith("ev_"))
                            {
                                isSubscriber = true;
                            }
                            // 2. Ищем явные префиксы вызывальщиков
                            else if (fieldName.StartsWith("call_") || fieldName.Contains("raise") || fieldName.Contains("invoke"))
                            {
                                isInvoker = true;
                            }
                            // 3. Если это наш SignalBinder, то по умолчанию это подписчик
                            else if (inheritSignalBinder)
                            {
                                isSubscriber = true;
                            }
                            // 4. По умолчанию для обычных скриптов - вызывальщик
                            else
                            {
                                isInvoker = true; 
                            }
                        }
                    }
                }
                type = type.BaseType;
            }

            if (isSubscriber) _sceneSubscribers.Add(script);
            if (isInvoker) _sceneInvokers.Add(script);
        }
    }

    // ───────── Find in project ─────────
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