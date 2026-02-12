using UnityEngine;
using UnityEditor;

public class TreePlacementTool : EditorWindow
{
    private GameObject treePrefab;
    private float embedDepth = 0.5f;
    private bool isPlacing = false;
    
    // Random Height Settings (Y axis only)
    private float minHeight = 0.8f;
    private float maxHeight = 1.2f;
    
    // Random Rotation Settings
    private bool randomYRotation = true;
    
    [MenuItem("Tools/Tree Placement Tool")]
    public static void ShowWindow()
    {
        GetWindow<TreePlacementTool>("Tree Placement Tool");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tree Placement Settings", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        treePrefab = (GameObject)EditorGUILayout.ObjectField(
            "Tree Prefab",
            treePrefab,
            typeof(GameObject),
            false
        );
        
        embedDepth = EditorGUILayout.Slider("Embed Depth", embedDepth, 0f, 5f);
        
        EditorGUILayout.Space();
        
        // Random Height Section
        GUILayout.Label("Random Height Settings (Y Axis)", EditorStyles.boldLabel);
        
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Min Height:", GUILayout.Width(80));
        minHeight = EditorGUILayout.FloatField(minHeight, GUILayout.Width(60));
        GUILayout.Label("Max Height:", GUILayout.Width(80));
        maxHeight = EditorGUILayout.FloatField(maxHeight, GUILayout.Width(60));
        EditorGUILayout.EndHorizontal();
        
        // Clamp values to prevent errors
        if (minHeight < 0.01f) minHeight = 0.01f;
        if (maxHeight < minHeight) maxHeight = minHeight;
        
        EditorGUILayout.LabelField($"Height Range: {minHeight:F2} - {maxHeight:F2}", EditorStyles.miniLabel);
        
        EditorGUILayout.Space();
        
        // Random Rotation Section
        GUILayout.Label("Random Rotation Settings", EditorStyles.boldLabel);
        randomYRotation = EditorGUILayout.Toggle("Random Y Rotation", randomYRotation);
        
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Start Placing Trees"))
        {
            if (treePrefab != null)
            {
                isPlacing = true;
                SceneView.duringSceneGui -= OnSceneGUI;
                SceneView.duringSceneGui += OnSceneGUI;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a tree prefab first!", "OK");
            }
        }
        
        if (GUILayout.Button("Stop Placing"))
        {
            isPlacing = false;
            SceneView.duringSceneGui -= OnSceneGUI;
        }
        
        GUILayout.Label("\nInstructions:");
        GUILayout.Label("- Assign a tree prefab");
        GUILayout.Label("- Adjust embed depth as needed");
        GUILayout.Label("- Set min/max height for random Y scaling");
        GUILayout.Label("- Enable random Y rotation if desired");
        GUILayout.Label("- Click 'Start Placing Trees'");
        GUILayout.Label("- Click on terrain to place trees");
        GUILayout.Label("- Press 'Stop Placing' when done");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPlacing || treePrefab == null) return;

        Event e = Event.current;
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                // Check if we hit a terrain
                if (hit.collider.GetComponent<Terrain>() != null)
                {
                    PlaceTreeAtPosition(hit.point);
                    e.Use();
                }
            }
        }
        
        // Draw placement preview
        if (e.type == EventType.MouseMove)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.collider.GetComponent<Terrain>() != null)
                {
                    Handles.color = Color.green;
                    Handles.DrawWireCube(
                        hit.point - Vector3.up * embedDepth,
                        new Vector3(0.5f, 1.0f + embedDepth * 2, 0.5f)
                    );
                    Handles.color = Color.white;
                    SceneView.RepaintAll();
                }
            }
        }
    }

    private void PlaceTreeAtPosition(Vector3 worldPosition)
    {
        if (treePrefab != null)
        {
            // Adjust position based on embed depth
            Vector3 adjustedPosition = worldPosition - Vector3.up * embedDepth;
            
            GameObject newTree = PrefabUtility.InstantiatePrefab(treePrefab) as GameObject;
            newTree.transform.position = adjustedPosition;
            
            // Apply random rotation on Y axis
            if (randomYRotation)
            {
                float randomRotation = Random.Range(0f, 360f);
                newTree.transform.rotation = Quaternion.Euler(0f, randomRotation, 0f);
            }
            else
            {
                newTree.transform.rotation = Quaternion.identity;
            }
            
            // Apply random height (Y axis only)
            float randomHeight = Random.Range(minHeight, maxHeight);
            Vector3 currentScale = newTree.transform.localScale;
            newTree.transform.localScale = new Vector3(currentScale.x, currentScale.y * randomHeight, currentScale.z);
            
            Undo.RegisterCreatedObjectUndo(newTree, "Place Tree");
            
            // We don't set parent to terrain since currentTerrain wasn't initialized
            // Just leave it as a world object
        }
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }
}