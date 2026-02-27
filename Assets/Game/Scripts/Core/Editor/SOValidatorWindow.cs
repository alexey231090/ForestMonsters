using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class SOValidatorWindow : EditorWindow
{
    private enum Tab { Validator, SettingsExplorer, FolderColours }
    private Tab _currentTab = Tab.Validator;
    private string[] _tabNames = { "🔍 Валидатор", "⚙️ Настройки Проекта", "🎨 Папки" };

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
    private Vector2 _folderScrollPos;
    private string _settingsSearch = "";
    
    // Фильтры
    private bool _filterEvents = true;
    private bool _filterVariables = true;
    private bool _filterSettings = true;

    // --- Настройки путей ---
    private const string PrefKeyAutoMode = "SOValidator_AutoMode";
    private const string PrefKeyEventsPath = "SOValidator_EventsPath";
    private const string PrefKeyVariablesPath = "SOValidator_VariablesPath";
    private const string PrefKeySettingsPath = "SOValidator_SettingsPath";

    private bool _autoMode = true;
    private string _eventsPath = "";
    private string _variablesPath = "";
    private string _settingsPath = "";
    private bool _showConfig = false;

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
        LoadPrefs();
        RefreshSettingsList();
    }

    private void LoadPrefs()
    {
        _autoMode = EditorPrefs.GetBool(PrefKeyAutoMode, true);
        _eventsPath = EditorPrefs.GetString(PrefKeyEventsPath, "Assets");
        _variablesPath = EditorPrefs.GetString(PrefKeyVariablesPath, "Assets");
        _settingsPath = EditorPrefs.GetString(PrefKeySettingsPath, "Assets");
    }

    private void SavePrefs()
    {
        EditorPrefs.SetBool(PrefKeyAutoMode, _autoMode);
        EditorPrefs.SetString(PrefKeyEventsPath, _eventsPath);
        EditorPrefs.SetString(PrefKeyVariablesPath, _variablesPath);
        EditorPrefs.SetString(PrefKeySettingsPath, _settingsPath);
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
            case Tab.FolderColours:
                DrawFolderColorsTab();
                break;
        }
    }

    // ================== Вкладка: ОКРАСКА ПАПОК ==================

    private void DrawFolderColorsTab()
    {
        var entries = FolderColorizer.GetEntries();

        EditorGUILayout.HelpBox("🎨 Щёлкните '+ Добавить папку', выберите цвет и нажмите '✅ Применить'.", MessageType.None);
        EditorGUILayout.Space(4);

        // Кнопка добавить папку
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Добавить папку", GUILayout.Height(30)))
        {
            string selected = EditorUtility.OpenFolderPanel("Выберите папку", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                // Конвертируем абсолютный путь в относительный (Assets/...)
                if (selected.StartsWith(Application.dataPath))
                    selected = "Assets" + selected.Substring(Application.dataPath.Length);

                FolderColorizer.AddFolder(selected);
            }
        }
        if (GUILayout.Button("✅ Применить", GUILayout.Height(30), GUILayout.Width(110)))
        {
            FolderColorizer.SaveData();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        if (entries.Count == 0)
        {
            EditorGUILayout.HelpBox("Папок пока нет. Добавьте первую!", MessageType.None);
            return;
        }

        _folderScrollPos = EditorGUILayout.BeginScrollView(_folderScrollPos);

        int toRemove = -1;
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            string folderPath = FolderColorizer.GuidToPath(entry.guid);
            Color entryColor = FolderColorizer.GetColor(entry);

            // Карточка папки
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Цветная полоска сверху
            Rect headerRect = GUILayoutUtility.GetRect(0, 5, GUILayout.ExpandWidth(true));
            Color prevColor = GUI.color;
            GUI.color = new Color(entryColor.r, entryColor.g, entryColor.b, 1f);
            GUI.DrawTexture(headerRect, EditorGUIUtility.whiteTexture);
            GUI.color = prevColor;

            EditorGUILayout.Space(2);

            // Строка: иконка + путь + кнопка Ping
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("📁", GUILayout.Width(20));
            EditorGUILayout.LabelField(string.IsNullOrEmpty(folderPath) ? entry.guid + " (?)" : folderPath, EditorStyles.boldLabel);
            if (GUILayout.Button("Ping", GUILayout.Width(45)))
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(folderPath);
                if (obj != null) EditorGUIUtility.PingObject(obj);
            }
            if (GUILayout.Button("❌", GUILayout.Width(28)))
                toRemove = i;
            EditorGUILayout.EndHorizontal();

            // Строка: цвет + ярлык
            EditorGUILayout.BeginHorizontal();
            Color newColor = EditorGUILayout.ColorField("Цвет", entryColor, GUILayout.Width(200));
            if (newColor != entryColor)
                FolderColorizer.SetColor(i, newColor);

            string newLabel = EditorGUILayout.TextField("Ярлык", entry.label);
            if (newLabel != entry.label)
                FolderColorizer.SetLabel(i, newLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        EditorGUILayout.EndScrollView();

        if (toRemove >= 0)
        {
            FolderColorizer.RemoveAt(toRemove);
            FolderColorizer.SaveData();
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
        DrawConfigurationPanel();

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
            Type type = so.GetType();
            string path = AssetDatabase.GetAssetPath(so);
            
            bool passType = false;
            if (_filterEvents && IsEvent(type, path)) passType = true;
            if (_filterVariables && IsVariable(type, path)) passType = true;
            if (_filterSettings && IsSettings(type, path)) passType = true;

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
                string assetPath = AssetDatabase.GetAssetPath(so);
                string color = GetColorForType(so.GetType(), assetPath);
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

    private void DrawConfigurationPanel()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        _showConfig = EditorGUILayout.Foldout(_showConfig, "⚙️ Конфигурация путей", true, new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });
        if (_showConfig)
        {
            EditorGUI.BeginChangeCheck();
            
            _autoMode = EditorGUILayout.ToggleLeft("Автоматический режим (Умный поиск)", _autoMode);
            
            if (!_autoMode)
            {
                EditorGUILayout.Space(5);
                DrawPathSelector("Папка Событий:", ref _eventsPath);
                DrawPathSelector("Папка Переменных:", ref _variablesPath);
                DrawPathSelector("Папка Настроек:", ref _settingsPath);
            }

            if (EditorGUI.EndChangeCheck())
            {
                SavePrefs();
                RefreshSettingsList();
            }
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawPathSelector(string label, ref string path)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(130));
        path = EditorGUILayout.TextField(path);
        if (GUILayout.Button("Выбрать", GUILayout.Width(70)))
        {
            string absPath = EditorUtility.OpenFolderPanel("Выберите папку", Application.dataPath, "");
            if (!string.IsNullOrEmpty(absPath) && absPath.StartsWith(Application.dataPath))
            {
                path = "Assets" + absPath.Substring(Application.dataPath.Length);
                GUI.FocusControl(null);
            }
        }
        EditorGUILayout.EndHorizontal();
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

            // Добавляем только те, что действительно являются нашими типами (по любому из признаков)
            if (GetColorForType(so.GetType(), path) != "#ffffff")
            {
                _foundSettings.Add(so);
            }
        }
        _foundSettings = _foundSettings.OrderBy(s => s.GetType().Name).ThenBy(s => s.name).ToList();
    }

    // --- Вспомогательные методы ---

    private string GetTypePrettyName(Type t)
    {
        string path = ""; // В этом контексте путь редко нужен для лейбла, но для надежности проверим тип
        if (IsEvent(t, "")) return "EVENT";
        if (IsVariable(t, "")) return "VARIABLE";
        if (IsSettings(t, "")) return "SETTINGS";
        return t.Name.ToUpper();
    }

    private string GetColorForType(Type t, string path = "")
    {
        if (IsEvent(t, path)) return "#FF9800";
        if (IsVariable(t, path)) return "#4CAF50";
        if (IsSettings(t, path)) return "#55aaff";
        return "#ffffff";
    }

    // --- ЛОГИКА ОПРЕДЕЛЕНИЯ ТИПОВ ---

    private bool IsEvent(Type t, string path)
    {
        if (!_autoMode && !string.IsNullOrEmpty(path)) return !string.IsNullOrEmpty(_eventsPath) && path.StartsWith(_eventsPath);

        if (t.Name.Contains("Event")) return true;
        if (typeof(GameEvent).IsAssignableFrom(t)) return true;
        if (path.ToLower().Contains("/events/")) return true;
        return false;
    }

    private bool IsVariable(Type t, string path)
    {
        if (!_autoMode && !string.IsNullOrEmpty(path)) return !string.IsNullOrEmpty(_variablesPath) && path.StartsWith(_variablesPath);

        if (t.Name.Contains("Variable")) return true;
        if (typeof(ScriptableVariableBase).IsAssignableFrom(t)) return true;
        if (path.ToLower().Contains("/variables/")) return true;
        return false;
    }

    private bool IsSettings(Type t, string path)
    {
        if (!_autoMode && !string.IsNullOrEmpty(path)) return !string.IsNullOrEmpty(_settingsPath) && path.StartsWith(_settingsPath);

        // 1. По атрибуту (самый надежный способ)
        if (t.GetCustomAttribute<SOSettingsAttribute>() != null) return true;
        
        // 2. По имени
        if (t.Name.Contains("Settings")) return true;
        
        // 3. По папке, в которой лежит ассет
        if (!string.IsNullOrEmpty(path))
        {
            string lowPath = path.ToLower();
            if (lowPath.Contains("/settings/") || lowPath.Contains("/data/")) return true;
        }

        return false;
    }

    private Color HexToColor(string hex)
    {
        Color color = Color.white;
        if (ColorUtility.TryParseHtmlString(hex, out color)) return color;
        return Color.white;
    }
}
