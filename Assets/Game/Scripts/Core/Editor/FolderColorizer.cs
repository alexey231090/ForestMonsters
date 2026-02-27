using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Раскрашивает папки в Project View.
/// Данные хранятся в EditorPrefs как JSON.
/// Вызывается автоматически при старте редактора через [InitializeOnLoad].
/// </summary>
[InitializeOnLoad]
public static class FolderColorizer
{
    private const string PrefKey = "FolderColorizer_Data";

    // Структура данных для одной папки
    [Serializable]
    public class FolderEntry
    {
        public string guid;       // GUID папки (Assets/...)
        public string hexColor;   // Цвет в формате #RRGGBBAA
        public string label;      // Пользовательский ярлык
    }

    [Serializable]
    private class FolderData
    {
        public List<FolderEntry> folders = new List<FolderEntry>();
    }

    private static FolderData _data;

    // Статический конструктор — вызывается при загрузке редактора
    static FolderColorizer()
    {
        LoadData();
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
    }

    // ─── Public API ────────────────────────────────────────────────────────────

    public static List<FolderEntry> GetEntries() => _data.folders;

    public static void AddFolder(string path)
    {
        string guid = AssetDatabase.AssetPathToGUID(path);
        if (string.IsNullOrEmpty(guid)) return;

        // Не добавляем дублей
        foreach (var e in _data.folders)
            if (e.guid == guid) return;

        _data.folders.Add(new FolderEntry
        {
            guid = guid,
            hexColor = ColorToHex(new Color(1f, 0.8f, 0.2f, 0.45f)),
            label = System.IO.Path.GetFileName(path)
        });
        SaveData();
    }

    public static void RemoveAt(int index)
    {
        if (index >= 0 && index < _data.folders.Count)
        {
            _data.folders.RemoveAt(index);
            SaveData();
        }
    }

    public static void SetColor(int index, Color color)
    {
        if (index >= 0 && index < _data.folders.Count)
        {
            _data.folders[index].hexColor = ColorToHex(color);
        }
    }

    public static void SetLabel(int index, string label)
    {
        if (index >= 0 && index < _data.folders.Count)
        {
            _data.folders[index].label = label;
        }
    }

    public static void SaveData()
    {
        string json = JsonUtility.ToJson(_data);
        EditorPrefs.SetString(PrefKey, json);
        EditorApplication.RepaintProjectWindow();
    }

    public static void LoadData()
    {
        string json = EditorPrefs.GetString(PrefKey, "{}");
        _data = JsonUtility.FromJson<FolderData>(json) ?? new FolderData();
        if (_data.folders == null) _data.folders = new List<FolderEntry>();
    }

    public static Color GetColor(FolderEntry entry)
    {
        Color c;
        if (ColorUtility.TryParseHtmlString(entry.hexColor, out c)) return c;
        return new Color(1f, 0.8f, 0.2f, 0.45f);
    }

    public static string GuidToPath(string guid) => AssetDatabase.GUIDToAssetPath(guid);

    private static GUIStyle _labelStyle;

    private static GUIStyle GetLabelStyle()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
        }
        return _labelStyle;
    }

    // ─── Rendering ─────────────────────────────────────────────────────────────

    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        foreach (var entry in _data.folders)
        {
            if (entry.guid != guid) continue;

            Color col = GetColor(entry);

            // Определяем размер иконки: маленький (list view) или большой (grid view)
            bool isSmall = selectionRect.height <= 20f;

            Rect colorRect;
            if (isSmall)
            {
                colorRect = new Rect(selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height);
            }
            else
            {
                colorRect = selectionRect;
            }

            // Рисуем полупрозрачный фон
            Color prev = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(colorRect, EditorGUIUtility.whiteTexture);
            GUI.color = prev;

            // В grid view дополнительно рисуем яркую полоску снизу с лейблом
            if (!isSmall && !string.IsNullOrEmpty(entry.label))
            {
                Color labelColor = col;
                labelColor.a = 0.85f;
                Rect labelRect = new Rect(selectionRect.x, selectionRect.yMax - 16f, selectionRect.width, 16f);
                GUI.color = labelColor;
                GUI.DrawTexture(labelRect, EditorGUIUtility.whiteTexture);
                GUI.color = prev;

                GUI.Label(labelRect, entry.label, GetLabelStyle());
            }

            break;
        }
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static string ColorToHex(Color c)
    {
        return "#" + ColorUtility.ToHtmlStringRGBA(c);
    }
}
