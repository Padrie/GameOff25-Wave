using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DoorSpawner : MonoBehaviour
{
    [System.Serializable]
    public class DoorSpawnRule
    {
        public GameObject doorPrefab;
        public string spawnPointPrefix = "Off";
    }

    [Header("Door Spawn Configuration")]
    [SerializeField] private DoorSpawnRule[] doorRules;

    [Header("Spawn Settings")]
    [SerializeField] private bool searchEntireScene = true;
    [SerializeField] private bool searchInChildren = true;
    [SerializeField] private bool clearExistingDoors = true;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    public void SpawnDoors()
    {
        if (doorRules == null || doorRules.Length == 0)
        {
            Debug.LogWarning("No door rules configured in DoorSpawner!");
            return;
        }

        int totalSpawned = 0;

        foreach (var rule in doorRules)
        {
            if (rule.doorPrefab == null)
            {
                Debug.LogWarning($"Door prefab is null for rule with prefix '{rule.spawnPointPrefix}'");
                continue;
            }

            totalSpawned += SpawnDoorsForRule(rule);
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=green>Door spawning complete! Total doors spawned: {totalSpawned}</color>");
        }
    }

    private int SpawnDoorsForRule(DoorSpawnRule rule)
    {
        GameObject[] spawnPoints = FindSpawnPoints(rule.spawnPointPrefix);
        int spawnedCount = 0;

        foreach (GameObject spawnPoint in spawnPoints)
        {
            if (clearExistingDoors)
            {
                ClearChildDoors(spawnPoint);
            }

            GameObject spawnedDoor = InstantiateDoor(rule, spawnPoint);

            if (spawnedDoor != null)
            {
                spawnedCount++;

                if (showDebugLogs)
                {
                    Debug.Log($"Spawned '{rule.doorPrefab.name}' at '{spawnPoint.name}'");
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
                obj.name.IndexOf("Door", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                matchingObjects.Add(obj);
            }
        }

        return matchingObjects.ToArray();
    }

    private GameObject InstantiateDoor(DoorSpawnRule rule, GameObject spawnPoint)
    {
#if UNITY_EDITOR
        GameObject doorInstance = (GameObject)PrefabUtility.InstantiatePrefab(rule.doorPrefab);
#else
        GameObject doorInstance = Instantiate(rule.doorPrefab);
#endif

        if (doorInstance != null)
        {
            doorInstance.transform.SetParent(spawnPoint.transform);
            doorInstance.transform.localPosition = Vector3.zero;
            doorInstance.transform.localRotation = Quaternion.identity;
            doorInstance.transform.localScale = rule.doorPrefab.transform.localScale;
        }

        return doorInstance;
    }

    private void ClearChildDoors(GameObject spawnPoint)
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

    public void ClearAllDoors()
    {
        if (doorRules == null) return;

        int clearedCount = 0;

        foreach (var rule in doorRules)
        {
            GameObject[] spawnPoints = FindSpawnPoints(rule.spawnPointPrefix);

            foreach (GameObject spawnPoint in spawnPoints)
            {
                int childCount = spawnPoint.transform.childCount;
                ClearChildDoors(spawnPoint);
                clearedCount += childCount;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"<color=yellow>Cleared {clearedCount} door(s)</color>");
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(DoorSpawner))]
public class DoorSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DoorSpawner spawner = (DoorSpawner)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Door Spawning Controls", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Spawn/Reload Doors", GUILayout.Height(30)))
        {
            Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Spawn Doors");
            spawner.SpawnDoors();
            EditorUtility.SetDirty(spawner.gameObject);
        }

        GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
        if (GUILayout.Button("Clear All Doors", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog("Clear All Doors",
                "Are you sure you want to clear all spawned doors?",
                "Yes", "Cancel"))
            {
                Undo.RegisterFullObjectHierarchyUndo(spawner.gameObject, "Clear Doors");
                spawner.ClearAllDoors();
                EditorUtility.SetDirty(spawner.gameObject);
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.EndHorizontal();
    }
}
#endif