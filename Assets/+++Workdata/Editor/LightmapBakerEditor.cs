using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

[CustomEditor(typeof(LightmapBaker))]
public class LightmapBakerEditor : Editor
{
    private SerializedProperty objectsToBakeProperty;
    private SerializedProperty reflectionProbesToBakeProperty;
    private SerializedProperty lightmapSettingsProperty;

    private Dictionary<GameObject, StaticEditorFlags> originalStaticFlags = new Dictionary<GameObject, StaticEditorFlags>();
    private Dictionary<ReflectionProbe, ReflectionProbeMode> originalProbeModes = new Dictionary<ReflectionProbe, ReflectionProbeMode>();

    // Store complete lightmap data including textures
    private Lightmapping.GIWorkflowMode originalGIWorkflowMode;
    private LightmapData[] originalLightmaps;
    private Dictionary<Renderer, RendererLightmapData> preservedRendererData = new Dictionary<Renderer, RendererLightmapData>();

    private class RendererLightmapData
    {
        public int lightmapIndex;
        public Vector4 lightmapScaleOffset;
        public int realtimeLightmapIndex;
        public Vector4 realtimeLightmapScaleOffset;
    }

    private void OnEnable()
    {
        objectsToBakeProperty = serializedObject.FindProperty("objectsToBake");
        reflectionProbesToBakeProperty = serializedObject.FindProperty("reflectionProbesToBake");
        lightmapSettingsProperty = serializedObject.FindProperty("lightmapSettings");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(lightmapSettingsProperty, new GUIContent("Lightmap Settings"));
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Objects To Bake", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All", GUILayout.Height(25)))
        {
            for (int i = 0; i < objectsToBakeProperty.arraySize; i++)
            {
                objectsToBakeProperty.GetArrayElementAtIndex(i).FindPropertyRelative("shouldBake").boolValue = true;
            }
        }
        if (GUILayout.Button("Deselect All", GUILayout.Height(25)))
        {
            for (int i = 0; i < objectsToBakeProperty.arraySize; i++)
            {
                objectsToBakeProperty.GetArrayElementAtIndex(i).FindPropertyRelative("shouldBake").boolValue = false;
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(objectsToBakeProperty, true);

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(reflectionProbesToBakeProperty, true);

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Bake Lightmaps", GUILayout.Height(40)))
        {
            BakeLightmaps();
        }
    }

    private void BakeLightmaps()
    {
        LightmapBaker baker = (LightmapBaker)target;

        if (baker.lightmapSettings == null)
        {
            EditorUtility.DisplayDialog("Lightmap Baker", "Lightmap Settings must be assigned!", "OK");
            return;
        }

        if (baker.objectsToBake == null || baker.objectsToBake.Length == 0)
        {
            EditorUtility.DisplayDialog("Lightmap Baker", "No objects or probes assigned for baking!", "OK");
            return;
        }

        // Store original GI workflow mode and lightmaps
        originalGIWorkflowMode = Lightmapping.giWorkflowMode;
        originalLightmaps = LightmapSettings.lightmaps;

        // Store lightmap data for objects we want to preserve
        StoreLightmapData(baker);

        // Set to iterative mode to prevent clearing existing lightmaps
        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.Iterative;

        // Prepare objects for baking
        PrepareLightmapBake(baker);
        PrepareReflectionProbes(baker);

        // Perform the bake
        PerformBake();

        // Restore everything
        RestoreLightmapData();
        RestoreStaticFlags();
        RestoreReflectionProbes();

        // Restore original GI workflow mode
        Lightmapping.giWorkflowMode = originalGIWorkflowMode;
    }

    private void StoreLightmapData(LightmapBaker baker)
    {
        preservedRendererData.Clear();

        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null) continue;

            GameObject obj = renderer.gameObject;

            // Check if this object should be baked
            bool shouldBakeThisObject = System.Array.Exists(baker.objectsToBake,
                element => element != null && element.gameObject == obj && element.shouldBake);

            // Check if it's a child of a bakeable object
            bool isChildOfBakeObject = false;
            if (!shouldBakeThisObject)
            {
                foreach (BakeableObject bakeableObj in baker.objectsToBake)
                {
                    if (bakeableObj != null && bakeableObj.shouldBake && bakeableObj.gameObject != null)
                    {
                        if (IsChildOf(obj.transform, bakeableObj.gameObject.transform))
                        {
                            isChildOfBakeObject = true;
                            break;
                        }
                    }
                }
            }

            // If this object should NOT be baked and has existing lightmap data, store it
            if (!shouldBakeThisObject && !isChildOfBakeObject && renderer.lightmapIndex != -1)
            {
                RendererLightmapData data = new RendererLightmapData
                {
                    lightmapIndex = renderer.lightmapIndex,
                    lightmapScaleOffset = renderer.lightmapScaleOffset,
                    realtimeLightmapIndex = renderer.realtimeLightmapIndex,
                    realtimeLightmapScaleOffset = renderer.realtimeLightmapScaleOffset
                };
                preservedRendererData[renderer] = data;

                Debug.Log($"Storing lightmap data for: {renderer.gameObject.name} (Index: {data.lightmapIndex})");
            }
        }

        Debug.Log($"Stored lightmap data for {preservedRendererData.Count} objects to preserve");
    }

    private void RestoreLightmapData()
    {
        // First, ensure we have the original lightmaps available
        if (originalLightmaps != null && originalLightmaps.Length > 0)
        {
            // Merge original lightmaps with new ones
            List<LightmapData> mergedLightmaps = new List<LightmapData>(LightmapSettings.lightmaps);

            // Make sure all original lightmap indices are available
            for (int i = 0; i < originalLightmaps.Length; i++)
            {
                if (i >= mergedLightmaps.Count)
                {
                    mergedLightmaps.Add(originalLightmaps[i]);
                }
                else if (mergedLightmaps[i].lightmapColor == null && originalLightmaps[i].lightmapColor != null)
                {
                    // If the slot is empty but we have original data, use it
                    mergedLightmaps[i] = originalLightmaps[i];
                }
            }

            LightmapSettings.lightmaps = mergedLightmaps.ToArray();
        }

        // Restore renderer lightmap data
        foreach (var kvp in preservedRendererData)
        {
            Renderer renderer = kvp.Key;
            RendererLightmapData data = kvp.Value;

            if (renderer != null)
            {
                renderer.lightmapIndex = data.lightmapIndex;
                renderer.lightmapScaleOffset = data.lightmapScaleOffset;
                renderer.realtimeLightmapIndex = data.realtimeLightmapIndex;
                renderer.realtimeLightmapScaleOffset = data.realtimeLightmapScaleOffset;

                Debug.Log($"Restored lightmap data for: {renderer.gameObject.name} (Index: {data.lightmapIndex})");
            }
        }

        Debug.Log($"Restored lightmap data for {preservedRendererData.Count} objects");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void PrepareLightmapBake(LightmapBaker baker)
    {
        originalStaticFlags.Clear();

        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            StaticEditorFlags originalFlags = GameObjectUtility.GetStaticEditorFlags(obj);
            originalStaticFlags[obj] = originalFlags;

            bool shouldBakeThisObject = System.Array.Exists(baker.objectsToBake,
                element => element != null && element.gameObject == obj && element.shouldBake);

            bool isChildOfBakeObject = false;
            if (!shouldBakeThisObject)
            {
                foreach (BakeableObject bakeableObj in baker.objectsToBake)
                {
                    if (bakeableObj != null && bakeableObj.shouldBake && bakeableObj.gameObject != null)
                    {
                        if (IsChildOf(obj.transform, bakeableObj.gameObject.transform))
                        {
                            isChildOfBakeObject = true;
                            break;
                        }
                    }
                }
            }

            // Disable ContributeGI for objects that shouldn't be baked
            if (!shouldBakeThisObject && !isChildOfBakeObject)
            {
                StaticEditorFlags newFlags = originalFlags & ~StaticEditorFlags.ContributeGI;
                GameObjectUtility.SetStaticEditorFlags(obj, newFlags);
            }
        }

        Debug.Log($"Prepared {originalStaticFlags.Count} objects for baking");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private bool IsChildOf(Transform child, Transform parent)
    {
        Transform current = child.parent;
        while (current != null)
        {
            if (current == parent)
                return true;
            current = current.parent;
        }
        return false;
    }

    private void PerformBake()
    {
        Debug.Log("Starting lightmap bake...");
        Lightmapping.Bake();
        Debug.Log("Lightmap bake completed");
    }

    private void RestoreStaticFlags()
    {
        foreach (var kvp in originalStaticFlags)
        {
            GameObject obj = kvp.Key;
            StaticEditorFlags originalFlags = kvp.Value;

            if (obj != null)
            {
                GameObjectUtility.SetStaticEditorFlags(obj, originalFlags);
            }
        }

        Debug.Log($"Restored static flags for {originalStaticFlags.Count} objects");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void PrepareReflectionProbes(LightmapBaker baker)
    {
        originalProbeModes.Clear();

        ReflectionProbe[] allProbes = FindObjectsOfType<ReflectionProbe>();

        foreach (ReflectionProbe probe in allProbes)
        {
            originalProbeModes[probe] = probe.mode;

            bool shouldBakeThisProbe = System.Array.Exists(baker.reflectionProbesToBake,
                element => element == probe);

            if (!shouldBakeThisProbe)
            {
                probe.mode = ReflectionProbeMode.Custom;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private void RestoreReflectionProbes()
    {
        foreach (var kvp in originalProbeModes)
        {
            ReflectionProbe probe = kvp.Key;
            ReflectionProbeMode originalMode = kvp.Value;

            if (probe != null)
            {
                probe.mode = originalMode;
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}