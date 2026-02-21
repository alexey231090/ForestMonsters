using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;

[CustomEditor(typeof(SignalBinder), true)]
[CanEditMultipleObjects]
public class SignalBinderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // 1. Отрисовываем стандартные публичные поля / [SerializeField]
        DrawDefaultInspector();

        // 2. Ищем приватные поля с атрибутом [Bind], которые НЕ помечены [SerializeField]
        // (SerializeField поля и так отрисовались выше)
        Type type = target.GetType();
        FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        bool headerDrawn = false;

        foreach (var field in fields)
        {
            var bindAttr = field.GetCustomAttribute<BindAttribute>();
            var serializeAttr = field.GetCustomAttribute<SerializeField>();

            // Если есть [Bind], но НЕТ [SerializeField] (чтобы не дублировать)
            if (bindAttr != null && serializeAttr == null)
            {
                bool isValidType = typeof(ScriptableVariableBase).IsAssignableFrom(field.FieldType) || 
                                 typeof(GameEvent).IsAssignableFrom(field.FieldType);

                if (isValidType)
                {
                    if (!headerDrawn)
                    {
                        EditorGUILayout.Space();
                        EditorGUILayout.LabelField("Magic Bindings", EditorStyles.boldLabel);
                        headerDrawn = true;
                    }

                    EditorGUI.BeginChangeCheck();
                    
                    UnityEngine.Object oldValue = field.GetValue(target) as UnityEngine.Object;
                    
                    UnityEngine.Object newValue = EditorGUILayout.ObjectField(
                        ObjectNames.NicifyVariableName(field.Name), 
                        oldValue, 
                        field.FieldType, 
                        true
                    );

                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(target, "Change " + field.Name);
                        field.SetValue(target, newValue);
                        EditorUtility.SetDirty(target);
                    }
                }
            }
        }
    }
}
