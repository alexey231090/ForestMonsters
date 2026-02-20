using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class SOValidatorWindow : EditorWindow
{
    private enum Tab { Validator, SettingsExplorer }
    private Tab _currentTab = Tab.Validator;
    private string[] _tabNames = { "🔍 Валидатор", "⚙️ Настройки Проекта" };

    // --- Валидатор Данные ---
    private class ValidationError
    {
        public GameObject originObject;
        public MonoBehaviour component;
        public string fieldName;
        public Type fieldType;
    }
    private List<ValidationError> _errors = new List<ValidationError>();
    private Vector2 _scrollPosValidator;

    // --- Обозреватель Настроек Данные ---
    private List<ScriptableObject> _foundSettings = new List<ScriptableObject>();
    private Vector2 _scrollPosSettings;
    private string _settingsSearch = "";
    
    // Фильтры
    private bool _filterEvents = true;
    private bool _filterVariables = true;
    private bool _filterSettings = true;

    [MenuItem("Tools/SO Validator & Explorer")]
    public static void ShowWindow()
    {
        // Ищем типы стандартных окон для автоматического докинга
        var inspectorType = Type.GetType("UnityEditor.InspectorWindow, UnityEditor");
        var hierarchyType = Type.GetType("UnityEditor.SceneHierarchyWindow, UnityEditor");
        
        // Открываем окно, указывая типы окон, рядом с которыми оно должно появиться как вкладка
        var window = GetWindow<SOValidatorWindow>("SO Tool", true, typeof(SceneView), inspectorType, hierarchyType);
        window.Show();
    }

    private void OnEnable()
    {
        // При открытии сразу ищем настройки
        RefreshSettingsList();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        _currentTab = (Tab)GUILayout.Toolbar((int)_currentTab, _tabNames, GUILayout.Height(25));
        EditorGUILayout.Space(10);

        switch (_currentTab)
        {
            case Tab.Validator:
                DrawValidator();
                break;
            case Tab.SettingsExplorer:
                DrawSettingsExplorer();
                break;
        }
    }

    // ================== Вкладка: ВАЛИДАТОР ==================

    private void DrawValidator()
    {
        EditorGUILayout.BeginVertical();
        
        EditorGUILayout.HelpBox("Ищет пустые (null) ссылки на События, Переменные и Настройки на сцене.", MessageType.None);
        
        if (GUILayout.Button("🔍 Найти ошибки на сцене", GUILayout.Height(40)))
        {
            ScanScene();
        }

        EditorGUILayout.Space(5);
        
        if (_errors.Count > 0)
        {
            EditorGUILayout.LabelField($"Найдено проблем: {_errors.Count}", EditorStyles.boldLabel);
            _scrollPosValidator = EditorGUILayout.BeginScrollView(_scrollPosValidator);
            foreach (var error in _errors)
            {
                DrawErrorCard(error);
            }
            EditorGUILayout.EndScrollView();
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Ошибок не найдено! ✨", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndVertical();
    }

    private void ScanScene()
    {
        _errors.Clear();
        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var script in allScripts)
        {
            if (script == null) continue;
            Type type = script.GetType();
            while (type != null && type != typeof(MonoBehaviour))
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                foreach (var field in fields)
                {
                    if (typeof(ScriptableObject).IsAssignableFrom(field.FieldType))
                    {
                        if (GetColorForType(field.FieldType) == "#ffffff") continue; // Фильтруем шум
                        if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null) continue;

                        object value = field.GetValue(script);
                        if (value == null || value.Equals(null))
                        {
                            _errors.Add(new ValidationError {
                                originObject = script.gameObject,
                                component = script,
                                fieldName = field.Name,
                                fieldType = field.FieldType
                            });
                        }
                    }
                }
                type = type.BaseType;
            }
        }
    }

    private void DrawErrorCard(ValidationError error)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        
        string typeColor = GetColorForType(error.fieldType);
        string typeLabel = GetTypePrettyName(error.fieldType);
        EditorGUILayout.LabelField($"<color={typeColor}><b>[{typeLabel}]</b></color>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(80));
        
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField($"<b>{error.originObject.name}</b>", new GUIStyle(EditorStyles.label) { richText = true });
        EditorGUILayout.LabelField($"<size=10><color=#DCDCAA>{error.fieldName}</color></size>", new GUIStyle(EditorStyles.label) { richText = true });
        EditorGUILayout.EndVertical();

        if (GUILayout.Button("ОК", GUILayout.Width(40), GUILayout.Height(30)))
        {
            Selection.activeGameObject = error.originObject;
            EditorGUIUtility.PingObject(error.originObject);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
    }

    // ================== Вкладка: НАСТРОЙКИ (Explorer) ==================

    private void DrawSettingsExplorer()
    {
        // Панель фильтров
        EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
        
        GUIStyle filterStyle = new GUIStyle(EditorStyles.label) { richText = true };

        _filterEvents = EditorGUILayout.ToggleLeft("<color=#FF9800>●</color> События", _filterEvents, filterStyle, GUILayout.Width(90));
        _filterVariables = EditorGUILayout.ToggleLeft("<color=#4CAF50>●</color> Переменные", _filterVariables, filterStyle, GUILayout.Width(100));
        _filterSettings = EditorGUILayout.ToggleLeft("<color=#55aaff>●</color> Настройки", _filterSettings, filterStyle, GUILayout.Width(100));
        
        GUILayout.FlexibleSpace();
        
        if (GUILayout.Button("🔄 Обновить", EditorStyles.miniButton, GUILayout.Width(70))) RefreshSettingsList();
        EditorGUILayout.EndHorizontal();

        // Поиск
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        _settingsSearch = EditorGUILayout.TextField(_settingsSearch, EditorStyles.toolbarSearchField);
        EditorGUILayout.EndHorizontal();

        _scrollPosSettings = EditorGUILayout.BeginScrollView(_scrollPosSettings);
        
        var filteredList = _foundSettings.Where(so => {
            if (so == null) return false;
            string typeName = so.GetType().Name;
            
            bool passType = false;
            if (_filterEvents && typeName.Contains("Event")) passType = true;
            if (_filterVariables && typeName.Contains("Variable")) passType = true;
            if (_filterSettings && typeName.Contains("Settings")) passType = true;

            if (!passType) return false;

            if (!string.IsNullOrEmpty(_settingsSearch))
            {
                return so.name.IndexOf(_settingsSearch, StringComparison.OrdinalIgnoreCase) >= 0;
            }
            return true;
        }).ToList();

        if (filteredList.Count > 0)
        {
            foreach (var so in filteredList)
            {
                if (so == null) continue;
                bool isSelected = Selection.activeObject == so;
                
                GUIStyle style = new GUIStyle(EditorStyles.label);
                if (isSelected) style.normal.textColor = Color.cyan;

                EditorGUILayout.BeginHorizontal(isSelected ? "selectionRect" : GUIStyle.none);
                
                // Иконка типа
                string color = GetColorForType(so.GetType());
                EditorGUILayout.LabelField(" ● ", new GUIStyle(EditorStyles.label) { normal = { textColor = HexToColor(color) } }, GUILayout.Width(20));
                
                if (GUILayout.Button($"{so.name}", style))
                {
                    Selection.activeObject = so;
                    EditorGUIUtility.PingObject(so);
                }
                
                EditorGUILayout.LabelField($"<size=9><color=#666666>{so.GetType().Name}</color></size>", new GUIStyle(EditorStyles.label) { richText = true }, GUILayout.Width(100));
                
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Настройки не найдены...", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshSettingsList()
    {
        _foundSettings.Clear();
        // Ищем только внутри папки Assets, чтобы исключить Packages и системные ассеты Unity
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" });
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // Дополнительная проверка пути (на случай, если Assets/Plugins содержит шум)
            if (path.Contains("/Plugins/") || path.Contains("/TextMesh Pro/")) continue;

            ScriptableObject so = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (so == null) continue;

            // Добавляем только те, что действительно являются нашими типами
            if (GetColorForType(so.GetType()) != "#ffffff")
            {
                _foundSettings.Add(so);
            }
        }
        _foundSettings = _foundSettings.OrderBy(s => s.GetType().Name).ThenBy(s => s.name).ToList();
    }

    // --- Вспомогательные методы ---

    private string GetTypePrettyName(Type t)
    {
        if (t.Name.Contains("Event")) return "EVENT";
        if (t.Name.Contains("Variable")) return "VARIABLE";
        if (t.Name.Contains("Settings")) return "SETTINGS";
        return t.Name.ToUpper();
    }

    private string GetColorForType(Type t)
    {
        string name = t.Name;
        if (name.Contains("Event")) return "#FF9800";
        if (name.Contains("Variable")) return "#4CAF50";
        if (name.Contains("Settings")) return "#55aaff";
        return "#ffffff";
    }

    private Color HexToColor(string hex)
    {
        Color color = Color.white;
        if (ColorUtility.TryParseHtmlString(hex, out color)) return color;
        return Color.white;
    }
}
