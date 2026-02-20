using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScriptableVariable<>), true)]
[CanEditMultipleObjects]
public class ScriptableVariableEditor : Editor
{
    private GUIStyle _valueStyle;

    public override void OnInspectorGUI()
    {
        // Стандартные поля (InitialValue, onValueChanged)
        DrawDefaultInspector();

        if (Application.isPlaying)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Value (Live)", EditorStyles.boldLabel);

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

            // Получаем через рефлексию текущее значение
            var valueProp = target.GetType().GetProperty("Value");
            if (valueProp != null)
            {
                object val = valueProp.GetValue(target);
                
                string displayValue = val != null ? val.ToString() : "null";

                EditorGUILayout.BeginVertical(_valueStyle);
                GUILayout.Space(10);
                EditorGUILayout.LabelField($"<color=#4CAF50>{displayValue}</color>", _valueStyle, GUILayout.Height(50));
                GUILayout.Space(10);
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);
                
                EditorGUILayout.HelpBox("Изменение значения через скрипты автоматически вызовет GameEvent 'onValueChanged'. Вы можете сделать это вручную ниже:", MessageType.Info);
                
                if (GUILayout.Button("⚡ Force Raise Signal", GUILayout.Height(30)))
                {
                    var raiseMethod = target.GetType().GetMethod("Raise");
                    raiseMethod?.Invoke(target, null);
                }
            }
        }
    }
}
