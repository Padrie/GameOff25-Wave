#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshSeparator))]
public class MeshSeparatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MeshSeparator separator = (MeshSeparator)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Mesh Analysis", EditorStyles.boldLabel);

        if (GUILayout.Button("Analyze Mesh", GUILayout.Height(30)))
        {
            int partCount = separator.GetLoosePartCount();
            Debug.Log($"Found {partCount} loose part(s) in the mesh.");
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Separate Loose Parts", GUILayout.Height(40)))
        {
            separator.SeparateLooseParts();
            Debug.Log("Mesh separation complete!");
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.HelpBox(
            "Click 'Analyze Mesh' to count loose parts, or 'Separate Loose Parts' to create individual objects.",
            MessageType.Info
        );
    }
}

/// <summary>
/// Menu item to quickly add mesh separator to selected objects
/// </summary>
public class MeshSeparatorMenu
{
    [MenuItem("GameObject/3D Object/Separate Mesh Parts", false, 0)]
    private static void SeparateMeshParts()
    {
        GameObject selected = Selection.activeGameObject;

        if (selected == null)
        {
            Debug.LogError("Please select a GameObject with a mesh first.");
            return;
        }

        MeshFilter mf = selected.GetComponent<MeshFilter>();
        if (mf == null || mf.sharedMesh == null)
        {
            Debug.LogError("Selected object doesn't have a valid mesh.");
            return;
        }

        MeshSeparator separator = selected.GetComponent<MeshSeparator>();
        if (separator == null)
        {
            separator = selected.AddComponent<MeshSeparator>();
        }

        separator.SeparateLooseParts();
    }

    [MenuItem("GameObject/3D Object/Separate Mesh Parts", true)]
    private static bool SeparateMeshPartsValidation()
    {
        return Selection.activeGameObject != null;
    }
}
#endif