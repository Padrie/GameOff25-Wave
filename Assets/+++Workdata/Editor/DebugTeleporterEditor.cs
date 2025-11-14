using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Editor window for managing debug teleport points.
/// Access via Window > Debug Teleporter Manager
/// </summary>
public class DebugTeleporterEditor : EditorWindow
{
    private DebugTeleporter teleporter;
    private Vector2 scrollPosition;
    private Transform playerTransform;

    [MenuItem("Window/Debug Teleporter Manager")]
    public static void ShowWindow()
    {
        GetWindow<DebugTeleporterEditor>("Teleport Manager");
    }

    void OnEnable()
    {
        FindTeleporter();
    }

    void OnGUI()
    {
        GUILayout.Label("Debug Teleporter Manager", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Find or create teleporter
        if (teleporter == null)
        {
            EditorGUILayout.HelpBox("No DebugTeleporter found in scene.", MessageType.Warning);

            if (GUILayout.Button("Create Debug Teleporter"))
            {
                CreateTeleporter();
            }

            if (GUILayout.Button("Find Existing Teleporter"))
            {
                FindTeleporter();
            }

            return;
        }

        // Player transform field
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Marker at Scene View Position"))
        {
            CreateMarkerAtSceneView();
        }

        if (GUILayout.Button("Select Teleporter in Hierarchy"))
        {
            Selection.activeGameObject = teleporter.gameObject;
            EditorGUIUtility.PingObject(teleporter.gameObject);
        }

        if (GUILayout.Button("Refresh Marker Count"))
        {
            teleporter.ForceRefreshMarkers();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // Count markers in scene
        TeleportMarker[] markers = FindObjectsOfType<TeleportMarker>();
        EditorGUILayout.HelpBox($"Found {markers.Length} TeleportMarker(s) in scene.", MessageType.Info);

        EditorGUILayout.Space();

        // Instructions
        EditorGUILayout.HelpBox(
            "✨ Markers are AUTOMATICALLY detected!\n\n" +
            "Runtime Controls:\n" +
            "• Press T to open teleport menu\n" +
            "• Press 1-9 to quick teleport to first 9 markers\n" +
            "• Press R to manually refresh marker list\n\n" +
            "Setup:\n" +
            "1. Create empty GameObjects in scene\n" +
            "2. Add TeleportMarker component\n" +
            "3. Name them clearly\n" +
            "4. That's it! They're auto-detected\n\n" +
            "Tip: Use number prefixes (01_, 02_) to control\n" +
            "which markers get 1-9 hotkeys (sorted alphabetically)",
            MessageType.Info
        );
    }

    void FindTeleporter()
    {
        teleporter = FindObjectOfType<DebugTeleporter>();
    }

    void CreateTeleporter()
    {
        GameObject go = new GameObject("DebugTeleporter");
        teleporter = go.AddComponent<DebugTeleporter>();

        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        Debug.Log("DebugTeleporter created! It will auto-detect TeleportMarker objects in the scene.");
    }

    void CreateMarkerAtSceneView()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
        {
            Debug.LogWarning("No active scene view found!");
            return;
        }

        GameObject marker = new GameObject("TeleportMarker");
        marker.AddComponent<TeleportMarker>();

        // Position at scene view camera
        marker.transform.position = sceneView.camera.transform.position;
        marker.transform.rotation = sceneView.camera.transform.rotation;

        Selection.activeGameObject = marker;
        SceneView.FrameLastActiveSceneView();

        Debug.Log("Created TeleportMarker at scene view position!");
    }
}