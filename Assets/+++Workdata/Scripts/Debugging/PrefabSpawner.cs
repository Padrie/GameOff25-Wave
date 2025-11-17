using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PrefabSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PrefabSpawnRule
    {
        public GameObject Prefab;
        public string spawnPointPrefix = "Off";
    }

    [Header("Prefab Spawn Configuration")]
    [SerializeField] private PrefabSpawnRule[] prefabRules;

    [Header("Spawn Settings")]
    [SerializeField] private bool searchEntireScene = true;
    [SerializeField] private bool searchInChildren = true;
    [SerializeField] private bool clearExistingPrefabs = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    public void SpawnPrefabs()
    {
        if (prefabRules == null || prefabRules.Length == 0)
        {
            Debug.LogWarning("No Prefab rules configured in PrefabSpawner!");
            return;
        }

        int totalSpawned = 0;

        foreach (var rule in prefabRules)
        {
            if (rule.Prefab == null)
            {
                Debug.LogWarning($"Prefab prefab is null for rule with prefix '{rule.spawnPointPrefix}'");
                continue;
            }

            totalSpawned += SpawnPrefabsForRule(rule);
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>Prefab spawning complete! Total Prefabs spawned: {totalSpawned}</color>");
        }
    }

    private int SpawnPrefabsForRule(PrefabSpawnRule rule)
    {
        GameObject[] spawnPoints = FindSpawnPoints(rule.spawnPointPrefix);
        int spawnedCount = 0;

        foreach (GameObject spawnPoint in spawnPoints)
        {
            if (clearExistingPrefabs)
            {
                ClearChildPrefabs(spawnPoint);
            }

            GameObject spawnedPrefab = InstantiatePrefab(rule, spawnPoint);

            if (spawnedPrefab != null)
            {
                spawnedCount++;

                if (showDebugLogs)
                {
                    Debug.Log($"Spawned '{rule.Prefab.name}' at '{spawnPoint.name}'");
                }
            }
        }

        return spawnedCount;
    }

    private GameObject[] FindSpawnPoints(string prefix)
    {
        GameObject[] allObjects;

        if (searchEntireScene)
        {
            allObjects = FindObjectsOfType<GameObject>(true);
        }
        else if (searchInChildren)
        {
            Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
            allObjects = new GameObject[allTransforms.Length];
            for (int i = 0; i < allTransforms.Length; i++)
            {
                allObjects[i] = allTransforms[i].gameObject;
            }
        }
        else
        {
            int childCount = transform.childCount;
            allObjects = new GameObject[childCount];
            for (int i = 0; i < childCount; i++)
            {
                allObjects[i] = transform.GetChild(i).gameObject;
            }
        }

        System.Collections.Generic.List<GameObject> matchingObjects = new System.Collections.Generic.List<GameObject>();
        string matchPrefix = prefix.Length >= 3 ? prefix.Substring(0, 3) : prefix;

        foreach (GameObject obj in allObjects)
        {
            if (obj != gameObject &&
                obj.name.Length >= 3 &&
                obj.name.Substring(0, 3).Equals(matchPrefix, System.StringComparison.OrdinalIgnoreCase) &&
                obj.name.IndexOf(prefix.ToString(), System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matchingObjects.Add(obj);
            }
        }

        return matchingObjects.ToArray();
    }

    private GameObject InstantiatePrefab(PrefabSpawnRule rule, GameObject spawnPoint)
    {
#if UNITY_EDITOR
        GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(rule.Prefab);
#else
        GameObject Instance = Instantiate(rule.Prefab);
#endif

        if (prefabInstance != null)
        {
            prefabInstance.transform.SetParent(spawnPoint.transform);
            prefabInstance.transform.localPosition = Vector3.zero;
            prefabInstance.transform.localRotation = Quaternion.identity;
            prefabInstance.transform.localScale = rule.Prefab.transform.localScale;
        }

        return prefabInstance;
    }

    private void ClearChildPrefabs(GameObject spawnPoint)
    {
        int childCount = spawnPoint.transform.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Transform child = spawnPoint.transform.GetChild(i);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(child.gameObject);
            }
            else
            {
                Destroy(child.gameObject);
            }
#else
            Destroy(child.gameObject);
#endif
        }
    }

    public void ClearAllPrefabs()
    {
        if (prefabRules == null) return;

        int clearedCount = 0;

        foreach (var rule in prefabRules)
        {
            GameObject[] spawnPoints = FindSpawnPoints(rule.spawnPointPrefix);

            foreach (GameObject spawnPoint in spawnPoints)
            {
                int childCount = spawnPoint.transform.childCount;
                ClearChildPrefabs(spawnPoint);
                clearedCount += childCount;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>Cleared {clearedCount} prefab(s)</color>");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PrefabSpawner))]
public class PrefabSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PrefabSpawner spawner = (PrefabSpawner)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Prefab Spawning Controls", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Spawn/Reload Prefabs", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Spawn Prefabs");
            spawner.SpawnPrefabs();
            EditorUtility.SetDirty(spawner.gameObject);
        }

        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Clear All Prefabs", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear All Prefabs",
                "Are you sure you want to clear all spawned Prefabs?",
                "Yes", "Cancel"))
            {
                Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Clear Prefab");
                spawner.ClearAllPrefabs();
                EditorUtility.SetDirty(spawner.gameObject);
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();
    }
}
#endif