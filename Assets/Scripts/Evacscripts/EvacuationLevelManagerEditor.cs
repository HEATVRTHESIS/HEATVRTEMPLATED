#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(EvacuationLevelManager))]
public class EvacuationLevelManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EvacuationLevelManager manager = (EvacuationLevelManager)target;
        
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Level Generation", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Level", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                manager.GenerateLevel();
            }
            else
            {
                Debug.LogWarning("Enter Play Mode to generate level");
            }
        }
        
        if (GUILayout.Button("Clear Level", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                manager.ClearLevel();
            }
        }
        
        EditorGUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "Setup Instructions:\n\n" +
            "1. Create empty GameObjects for each path plane\n" +
            "2. Add child empty objects as obstacle spawn points\n" +
            "3. Create SpawnPointConfigs for each department\n" +
            "4. Assign paths to each department\n" +
            "5. Create empty objects for roadblock positions\n" +
            "6. Assign obstacle prefabs", 
            MessageType.Info
        );
    }
}
#endif