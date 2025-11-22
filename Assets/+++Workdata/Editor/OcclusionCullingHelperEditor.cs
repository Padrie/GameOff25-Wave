using UnityEngine;
using UnityEditor;

/// <summary>
/// Custom editor for OcclusionCullingHelper that adds bake and clear buttons to the inspector.
/// This file should be placed in an Editor folder.
/// </summary>
[CustomEditor(typeof(OcclusionCullingHelper))]
public class OcclusionCullingHelperEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OcclusionCullingHelper helper = (OcclusionCullingHelper)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Occlusion Culling Controls", EditorStyles.boldLabel);
        
        EditorGUILayout.Space(5);

        // Bake Button
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Bake Occlusion Culling", GUILayout.Height(30)))
        {
            BakeOcclusion();
        }

        EditorGUILayout.Space(5);

        // Clear Button
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("Clear Occlusion Data", GUILayout.Height(30)))
        {
            ClearOcclusion();
        }

        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space(10);

        // Status information
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        bool hasOcclusionData = StaticOcclusionCulling.umbraDataSize > 0;
        string status = hasOcclusionData ? "Occlusion data is baked" : "No occlusion data";
        EditorGUILayout.LabelField("Current State:", status);
        
        if (hasOcclusionData)
        {
            EditorGUILayout.LabelField("Data Size:", FormatBytes(StaticOcclusionCulling.umbraDataSize));
        }
    }

    private void BakeOcclusion()
    {
        Debug.Log("Starting occlusion culling bake...");
        
        if (StaticOcclusionCulling.umbraDataSize > 0)
        {
            if (!EditorUtility.DisplayDialog(
                "Occlusion Data Exists",
                "Occlusion data already exists. Do you want to re-bake?",
                "Yes, Re-bake",
                "Cancel"))
            {
                return;
            }
        }

        StaticOcclusionCulling.Compute();
        Debug.Log("Occlusion culling bake completed!");
    }

    private void ClearOcclusion()
    {
        if (StaticOcclusionCulling.umbraDataSize == 0)
        {
            EditorUtility.DisplayDialog(
                "No Occlusion Data",
                "There is no occlusion data to clear.",
                "OK"
            );
            return;
        }

        if (EditorUtility.DisplayDialog(
            "Clear Occlusion Data",
            "Are you sure you want to clear all occlusion culling data?",
            "Yes, Clear",
            "Cancel"))
        {
            StaticOcclusionCulling.Clear();
            Debug.Log("Occlusion culling data cleared!");
        }
    }

    private string FormatBytes(int bytes)
    {
        if (bytes < 1024)
            return bytes + " B";
        else if (bytes < 1024 * 1024)
            return (bytes / 1024f).ToString("F2") + " KB";
        else
            return (bytes / (1024f * 1024f)).ToString("F2") + " MB";
    }
}
