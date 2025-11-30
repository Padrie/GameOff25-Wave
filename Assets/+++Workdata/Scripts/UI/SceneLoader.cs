using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Material markSceneSkybox;

    private void Awake()
    {
        StartCoroutine(LoadScenesAsync());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "InGame" || scene.name == "MainMenuScene")
        {
            RenderSettings.skybox = markSceneSkybox;
            RenderSettings.ambientIntensity = 0f;
            RenderSettings.reflectionIntensity = 0.02f;

        }
    }

    IEnumerator LoadScenesAsync()
    {
        string[] scenes =
        {
            "MainMenuScene",
            "OptionsMenuScene",
            "PauseMenuScene",
            "DeathScreenScene",
            "WinScreenScene"
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