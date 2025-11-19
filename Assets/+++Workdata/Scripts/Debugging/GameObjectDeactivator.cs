using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class GameObjectToggler : MonoBehaviour
{
    [Header("GameObject to Toggle")]
    public GameObject targetGameObject;

    public void ToggleGameObject()
    {
        if (targetGameObject != null)
        {
            targetGameObject.SetActive(!targetGameObject.activeSelf);
            Debug.Log($"{targetGameObject.name} is now {(targetGameObject.activeSelf ? "Active" : "Inactive")}");
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(GameObjectToggler))]
    public class GameObjectTogglerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GameObjectToggler toggler = (GameObjectToggler)target;

            EditorGUILayout.Space();

            if (GUILayout.Button("Toggle GameObject"))
            {
                if (toggler.targetGameObject != null)
                {
                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(toggler.targetGameObject);

                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        GameObject prefabAsset = PrefabUtility.LoadPrefabContents(prefabPath);

                        bool currentState = prefabAsset.activeSelf;
                        bool newState = !currentState;

                        prefabAsset.SetActive(newState);

                        PrefabUtility.SaveAsPrefabAsset(prefabAsset, prefabPath);
                        PrefabUtility.UnloadPrefabContents(prefabAsset);

                        Debug.Log($"Prefab {toggler.targetGameObject.name} toggled to {(newState ? "Active" : "Inactive")}");

                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        Undo.RecordObject(toggler.targetGameObject, "Toggle GameObject");
                        toggler.targetGameObject.SetActive(!toggler.targetGameObject.activeSelf);
                        EditorUtility.SetDirty(toggler.targetGameObject);
                        EditorSceneManager.MarkSceneDirty(toggler.targetGameObject.scene);

                        Debug.Log($"{toggler.targetGameObject.name} is now {(toggler.targetGameObject.activeSelf ? "Active" : "Inactive")}");
                    }
                }
                else
                {
                    Debug.LogWarning("No GameObject assigned!");
                }
            }
        }
    }
#endif
}