using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

[CustomEditor(typeof(LightmapBaker))]
public class LightmapBakerEditor : Editor
{
    private SerializedProperty objectsToBakeProperty;
    private SerializedProperty reflectionProbesToBakeProperty;
    private SerializedProperty lightmapSettingsProperty;

    private System.Collections.Generic.Dictionary<GameObject, StaticEditorFlags> originalStaticFlags =
        new System.Collections.Generic.Dictionary<GameObject, StaticEditorFlags>();

    private System.Collections.Generic.Dictionary<ReflectionProbe, ReflectionProbeMode> originalProbeModes =
        new System.Collections.Generic.Dictionary<ReflectionProbe, ReflectionProbeMode>();

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

        PrepareLightmapBake(baker);
        PrepareReflectionProbes(baker);
        PerformBake();
        RestoreStaticFlags();
        RestoreReflectionProbes();
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

            if (!shouldBakeThisObject && !isChildOfBakeObject)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer == null || renderer.lightmapIndex == -1)
                {
                    StaticEditorFlags newFlags = originalFlags & ~StaticEditorFlags.ContributeGI;
                    GameObjectUtility.SetStaticEditorFlags(obj, newFlags);
                }
            }
        }

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
        Lightmapping.Bake();
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