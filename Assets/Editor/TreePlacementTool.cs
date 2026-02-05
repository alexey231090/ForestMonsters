using UnityEngine;
using UnityEditor;

public class TreePlacementTool : EditorWindow
{
    private GameObject treePrefab;
    private float embedDepth = 0.5f;
    private bool isPlacing = false;
    
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
            newTree.transform.rotation = Quaternion.identity;
            
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