using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    private void Awake()
    {
        StartCoroutine(LoadScenesAsync());
    }

    IEnumerator LoadScenesAsync()
    {

        string[] scenes =
        {
            "MainMenuScene",
            "OptionsMenuScene",
            "PauseMenuScene",
            //"MarkScene"
        };

        HashSet<string> loadedOrLoading = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        int sceneCount = SceneManager.sceneCount;
        for (int i = 0; i < sceneCount; i++)
        {
            Scene s = SceneManager.GetSceneAt(i);
            if (!string.IsNullOrEmpty(s.name))
                loadedOrLoading.Add(s.name);
        }

        List<AsyncOperation> asyncOperations = new List<AsyncOperation>();

        foreach (string scene in scenes)
        {
            if (loadedOrLoading.Contains(scene))
            {
                continue;
            }

            AsyncOperation op = SceneManager.LoadSceneAsync(scene, LoadSceneMode.Additive);
            if (op == null)
            {
                continue;
            }

            loadedOrLoading.Add(scene);
            asyncOperations.Add(op);

        }

        bool allDone = false;
        while (!allDone)
        {
            allDone = true;
            foreach (AsyncOperation op in asyncOperations)
            {
                if (!op.isDone)
                {
                    allDone = false;
                    break;
                }
            }
            yield return null;
        }
    }
}
