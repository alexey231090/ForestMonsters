using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

[CustomEditor(typeof(ScriptableVariable<>), true)]
[CanEditMultipleObjects]
public class ScriptableVariableEditor : Editor
{
    private GUIStyle _valueStyle;
    private GUIStyle _headerStyle;
    private GUIStyle _depBoxStyle;
    private GUIStyle _signalStyle;

    private List<MonoBehaviour> _sceneReaders = new List<MonoBehaviour>();
    private List<MonoBehaviour> _sceneWriters = new List<MonoBehaviour>();

    private int _lang; // 0 = RU, 1 = EN

    private void OnEnable()
    {
        _lang = EditorPrefs.GetInt("SOToolsLang", 0);
        FindSceneReferences();
    }

    private void InitStyles()
    {
        if (_valueStyle == null)
        {
            _valueStyle = new GUIStyle(EditorStyles.helpBox)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                richText = true
            };
        }

        if (_headerStyle == null)
        {
            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                richText = true
            };
        }

        if (_depBoxStyle == null)
        {
            _depBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(8, 8, 4, 4),
                margin = new RectOffset(4, 4, 2, 2)
            };
        }

        if (_signalStyle == null)
        {
            _signalStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                richText = true,
                fontSize = 12
            };
        }
    }

    public override void OnInspectorGUI()
    {
        InitStyles();

        // Language Toolbar
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        int newLang = GUILayout.Toolbar(_lang, new string[] { "RU", "EN" }, GUILayout.Width(100));
        if (newLang != _lang)
        {
            _lang = newLang;
            EditorPrefs.SetInt("SOToolsLang", _lang);
        }
        EditorGUILayout.EndHorizontal();

        // Standard fields
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        // 1. RUNTIME VALUE
        if (Application.isPlaying)
        {
            string runtimeLabel = _lang == 0 ? "📊 ТЕКУЩЕЕ ЗНАЧЕНИЕ (Play Mode)" : "📊 RUNTIME VALUE (LIVE)";
            EditorGUILayout.LabelField(runtimeLabel, _headerStyle);

            var valueProp = target.GetType().GetProperty("Value");
            if (valueProp != null)
            {
                object val = valueProp.GetValue(target);
                string displayValue = val != null ? val.ToString() : "null";

                EditorGUILayout.BeginVertical(_valueStyle);
                GUILayout.Space(10);
                EditorGUILayout.LabelField($"<color=#4CAF50>{displayValue}</color>", _valueStyle, GUILayout.Height(40));
                GUILayout.Space(10);
                EditorGUILayout.EndVertical();

                string forceRaiseLabel = _lang == 0 ? "⚡ Вызвать обновление (Signal)" : "⚡ Force Raise Signal";
                if (GUILayout.Button(forceRaiseLabel, GUILayout.Height(25)))
                {
                    var raiseMethod = target.GetType().GetMethod("Raise");
                    raiseMethod?.Invoke(target, null);
                }
            }
        }

        EditorGUILayout.Space(15);

        // 2. DEPENDENCIES
        string depHeader = _lang == 0 ? "🔍 СВЯЗИ НА СЦЕНЕ" : "🔍 SCENE DEPENDENCIES";
        EditorGUILayout.LabelField(depHeader, _headerStyle);

        // --- READERS ---
        string readersLabel = _lang == 0 
            ? $"📥 <color=#4CAF50>ПОДПИСЧИКИ / СЛУШАТЕЛИ</color> ([Bind] / [OnChanged]): {_sceneReaders.Count}" 
            : $"📥 <color=#4CAF50>SUBSCRIBERS / LISTENERS</color> ([Bind] / [OnChanged]): {_sceneReaders.Count}";
            
        EditorGUILayout.LabelField(readersLabel, _headerStyle);
        if (_sceneReaders.Count > 0)
        {
            foreach (var reader in _sceneReaders)
            {
                DrawDependencyCard(reader, true);
            }
        }
        else 
        {
            string noReaders = _lang == 0 ? "Нет компонентов, подписанных на изменения." : "No components are listening for changes.";
            EditorGUILayout.HelpBox(noReaders, MessageType.None);
        }

        EditorGUILayout.Space(5);

        // --- WRITERS/USERS ---
        string writersLabel = _lang == 0 
            ? $"⚙️ <color=#55aaff>ПОЛЬЗОВАТЕЛИ / ПИСАТЕЛИ</color>: {_sceneWriters.Count}" 
            : $"⚙️ <color=#55aaff>USERS / WRITERS</color>: {_sceneWriters.Count}";
            
        EditorGUILayout.LabelField(writersLabel, _headerStyle);
        if (_sceneWriters.Count > 0)
        {
            foreach (var writer in _sceneWriters)
            {
                DrawDependencyCard(writer, false);
            }
        }
        else 
        {
            string noWriters = _lang == 0 ? "Нет компонентов, ссылающихся на эту переменную." : "No components are referencing this variable.";
            EditorGUILayout.HelpBox(noWriters, MessageType.None);
        }

        EditorGUILayout.Space(10);
        string refreshLabel = _lang == 0 ? "🔄 Обновить связи" : "🔄 Refresh Dependencies";
        if (GUILayout.Button(refreshLabel))
        {
            FindSceneReferences();
        }
    }

    private void DrawDependencyCard(MonoBehaviour mb, bool isReader)
    {
        EditorGUILayout.BeginVertical(_depBoxStyle);
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.ObjectField(mb.gameObject, typeof(GameObject), true);

        // Кнопка PING (не меняет Selection)
        if (GUILayout.Button("Ping", GUILayout.Width(45)))
        {
            EditorGUIUtility.PingObject(mb.gameObject);
        }

        EditorGUILayout.EndHorizontal();

        string typeName = mb.GetType().Name;
        string prefix = isReader ? "  📥" : "  ⚙️";
        string label = $"{prefix} <color=#4EC9B0>{typeName}</color>";
        EditorGUILayout.LabelField(label, _signalStyle);

        EditorGUILayout.EndVertical();
    }

    private void FindSceneReferences()
    {
        _sceneReaders.Clear();
        _sceneWriters.Clear();

        MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script == null) continue;

            System.Type type = script.GetType();
            bool hasReference = false;
            bool isReader = false;

            while (type != null && type != typeof(MonoBehaviour))
            {
                 var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                 foreach (var field in fields)
                 {
                     if (typeof(ScriptableVariableBase).IsAssignableFrom(field.FieldType))
                     {
                         var val = field.GetValue(script) as ScriptableVariableBase;
                         if (val == (ScriptableVariableBase)target)
                         {
                             hasReference = true;
                              // Check for [OnChanged] or [Bind] (both auto-subscribe to changes)
                              if (field.GetCustomAttribute<OnChangedAttribute>() != null ||
                                  field.GetCustomAttribute<BindAttribute>() != null)
                              {
                                  isReader = true;
                              }
                         }
                     }
                 }
                 type = type.BaseType;
            }

            if (hasReference)
            {
                if (isReader) _sceneReaders.Add(script);
                else _sceneWriters.Add(script);
            }
        }
    }
}
