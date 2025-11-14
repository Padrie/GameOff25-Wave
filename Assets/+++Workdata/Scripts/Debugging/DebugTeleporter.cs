using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Debug teleportation system for quickly moving to different areas in your scene
/// Automatically finds all TeleportMarker objects in the scene
/// Add this to any GameObject in your scene
/// </summary>
public class DebugTeleporter : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private bool enableInBuild = false; // Only works in editor by default
    [SerializeField] private bool autoRefreshMarkers = true; // Automatically refresh marker list

    [Header("Hotkeys (Editor Only)")]
    [SerializeField] private KeyCode menuKey = KeyCode.T;
    [SerializeField] private bool useNumberKeys = true; // Use 1-9 for quick teleport
    [SerializeField] private KeyCode refreshKey = KeyCode.R; // Refresh markers list

    private List<TeleportMarker> teleportMarkers = new List<TeleportMarker>();
    private bool showMenu = false;
    private Vector2 scrollPosition;
    private GUIStyle menuStyle;
    private GUIStyle buttonStyle;
    private GUIStyle headerStyle;
    private bool stylesInitialized = false;

    void Start()
    {
        // Auto-find player if not assigned
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
            else
            {
                // Try to find camera or character controller
                playerTransform = Camera.main?.transform;
                if (playerTransform == null)
                    playerTransform = FindObjectOfType<CharacterController>()?.transform;
            }
        }

        RefreshTeleportMarkers();
    }

    void RefreshTeleportMarkers()
    {
        teleportMarkers.Clear();
        TeleportMarker[] markers = FindObjectsOfType<TeleportMarker>();

        // Sort by name for consistent ordering
        teleportMarkers = markers.OrderBy(m => m.GetPointName()).ToList();

        Debug.Log($"Found {teleportMarkers.Count} teleport markers in scene");
    }

    void Update()
    {
#if UNITY_EDITOR
        if (!enableInBuild && !Application.isEditor) return;
#else
        if (!enableInBuild) return;
#endif

        // Toggle menu
        if (Input.GetKeyDown(menuKey))
        {
            showMenu = !showMenu;

            // Refresh markers when opening menu if auto-refresh is enabled
            if (showMenu && autoRefreshMarkers)
            {
                RefreshTeleportMarkers();
            }
        }

        // Manual refresh with hotkey
        if (Input.GetKeyDown(refreshKey))
        {
            RefreshTeleportMarkers();
        }

        // Quick teleport with number keys (works whether menu is open or closed)
        if (useNumberKeys)
        {
            for (int i = 0; i < Mathf.Min(9, teleportMarkers.Count); i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    TeleportToIndex(i);
                }
            }
        }
    }

    void OnGUI()
    {
#if UNITY_EDITOR
        if (!enableInBuild && !Application.isEditor) return;
#else
        if (!enableInBuild) return;
#endif

        if (!showMenu) return;

        // Initialize styles if needed
        if (!stylesInitialized || menuStyle == null)
        {
            InitializeStyles();
        }

        // Draw semi-transparent background
        GUI.Box(new Rect(10, 10, 350, Screen.height - 20), "", menuStyle);

        GUILayout.BeginArea(new Rect(20, 20, 330, Screen.height - 40));

        GUILayout.Label("DEBUG TELEPORTER", headerStyle);
        GUILayout.Space(10);

        if (playerTransform == null)
        {
            GUILayout.Label("⚠ No player transform assigned!", headerStyle);
            GUILayout.EndArea();
            return;
        }

        // Current position display
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
        GUILayout.Label($"Current Position: {playerTransform.position}", labelStyle);
        GUILayout.Space(5);

        // Refresh button
        GUILayout.BeginHorizontal();
        if (GUILayout.Button($"🔄 Refresh Markers [{refreshKey}]", buttonStyle, GUILayout.Height(30)))
        {
            RefreshTeleportMarkers();
        }

        // Auto-refresh toggle
        string autoRefreshText = autoRefreshMarkers ? "✓ Auto" : "Manual";
        if (GUILayout.Button(autoRefreshText, buttonStyle, GUILayout.Width(70), GUILayout.Height(30)))
        {
            autoRefreshMarkers = !autoRefreshMarkers;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Check if markers exist
        if (teleportMarkers.Count == 0)
        {
            GUIStyle boldStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            GUILayout.Label("⚠ No TeleportMarkers found in scene!", boldStyle);
            GUILayout.Space(5);
            GUILayout.Label("Create GameObjects with TeleportMarker component", labelStyle);
            GUILayout.EndArea();
            return;
        }

        // Teleport markers list
        GUIStyle boldLabelStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
        GUILayout.Label($"Teleport Markers ({teleportMarkers.Count}):", boldLabelStyle);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(Screen.height - 230));

        for (int i = 0; i < teleportMarkers.Count; i++)
        {
            if (teleportMarkers[i] == null)
            {
                continue; // Skip destroyed markers
            }

            GUILayout.BeginHorizontal();

            // Hotkey indicator
            string hotkey = i < 9 ? $"[{i + 1}]" : "[ ]";
            GUILayout.Label(hotkey, GUILayout.Width(30));

            // Teleport button with marker name
            string markerName = teleportMarkers[i].GetPointName();
            if (GUILayout.Button(markerName, buttonStyle, GUILayout.Height(25)))
            {
                TeleportToIndex(i);
            }

            // Show active indicator if marker is disabled
            if (!teleportMarkers[i].gameObject.activeInHierarchy)
            {
                GUILayout.Label("(disabled)", GUILayout.Width(60));
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(2);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);

        GUIStyle miniStyle = new GUIStyle(GUI.skin.label) { fontSize = 10 };
        GUILayout.Label($"Press [{menuKey}] to close", miniStyle);

        GUILayout.EndArea();
    }

    void InitializeStyles()
    {
        try
        {
            menuStyle = new GUIStyle();
            menuStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.9f));

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.padding = new RectOffset(5, 5, 5, 5);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 16;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.cyan;

            stylesInitialized = true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Failed to initialize GUI styles: {e.Message}");
        }
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    void TeleportToIndex(int index)
    {
        if (playerTransform == null)
        {
            Debug.LogWarning("No player transform assigned!");
            return;
        }

        if (index < 0 || index >= teleportMarkers.Count)
        {
            Debug.LogWarning($"Invalid teleport index: {index}");
            return;
        }

        TeleportMarker marker = teleportMarkers[index];

        if (marker == null)
        {
            Debug.LogWarning("Teleport marker is null! Refreshing marker list...");
            RefreshTeleportMarkers();
            return;
        }

        if (!marker.gameObject.activeInHierarchy)
        {
            Debug.LogWarning($"Teleport marker '{marker.GetPointName()}' is disabled!");
        }

        Vector3 targetPosition = marker.transform.position;
        Quaternion targetRotation = marker.transform.rotation;

        // Disable character controller if present
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            playerTransform.position = targetPosition;
            playerTransform.rotation = targetRotation;
            cc.enabled = true;
        }
        else
        {
            // Try Rigidbody
            Rigidbody rb = playerTransform.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = targetPosition;
                rb.rotation = targetRotation;
                rb.linearVelocity = Vector3.zero;
            }
            else
            {
                playerTransform.position = targetPosition;
                playerTransform.rotation = targetRotation;
            }
        }

        Debug.Log($"Teleported to: {marker.GetPointName()} at {targetPosition}");
    }

    // Public method to manually refresh markers
    [ContextMenu("Refresh Teleport Markers")]
    public void ForceRefreshMarkers()
    {
        RefreshTeleportMarkers();
    }
}