#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class WaveEditorController
{
    private static bool wasPlaying = false;

    static WaveEditorController()
    {
        //Subscribe to play mode state changes
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;

        //Disable waves immediately when script loads
        DisableAllWaveMaterials();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredEditMode:
                //Entered Edit Mode - disable all waves
                DisableAllWaveMaterials();
                Debug.Log("🛑 Wave materials disabled in Edit Mode");
                break;

            case PlayModeStateChange.ExitingPlayMode:
                //About to exit Play Mode - disable all waves
                DisableAllWaveMaterials();
                break;

            case PlayModeStateChange.EnteredPlayMode:
                //Entered Play Mode - waves will be controlled by CircularWaveSpawner
                Debug.Log("▶️ Play Mode - Waves controlled by CircularWaveSpawner");
                break;
        }
    }

    private static void OnEditorUpdate()
    {
        bool isPlaying = EditorApplication.isPlaying;

        //Detect transition from playing to not playing
        if (!isPlaying && wasPlaying)
        {
            DisableAllWaveMaterials();
        }

        //In Edit Mode, continuously ensure waves are disabled
        if (!isPlaying)
        {
            //Check every few frames to avoid performance impact
            if (EditorApplication.timeSinceStartup % 1.0 < 0.1)
            {
                EnsureWavesDisabledInEditMode();
            }
        }

        wasPlaying = isPlaying;
    }

    private static void DisableAllWaveMaterials()
    {
        //Find all materials in the scene using the wave shader
        Renderer[] allRenderers = Object.FindObjectsOfType<Renderer>();
        int disabledCount = 0;

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null || renderer.sharedMaterial == null) continue;

            Material mat = renderer.sharedMaterial;

            //Check if it's using the wave shader
            if (mat.shader.name.Contains("Lit with Waves"))
            {
                //Disable wave keyword
                mat.DisableKeyword("_ENABLE_WAVES");
                mat.SetFloat("_EnableWaves", 0f);

                //Set all wave amplitudes to 0
                for (int i = 1; i <= 4; i++)
                {
                    mat.SetFloat($"_Wave{i}Amplitude", 0f);
                }

                disabledCount++;
            }
        }

        if (disabledCount > 0)
        {
            //Debug.Log($"Disabled {disabledCount} wave material(s)");
        }
    }

    private static void EnsureWavesDisabledInEditMode()
    {
        //Continuously check and disable waves in Edit Mode
        Renderer[] allRenderers = Object.FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in allRenderers)
        {
            if (renderer == null || renderer.sharedMaterial == null) continue;

            Material mat = renderer.sharedMaterial;

            //Check if it's using the wave shader and if waves are enabled
            if (mat.shader.name.Contains("Lit with Waves"))
            {
                //Check if any wave is active
                bool anyWaveActive = false;
                for (int i = 1; i <= 4; i++)
                {
                    if (mat.GetFloat($"_Wave{i}Amplitude") > 0.001f)
                    {
                        anyWaveActive = true;
                        break;
                    }
                }

                //If any wave is active in Edit Mode, disable it
                if (anyWaveActive)
                {
                    mat.DisableKeyword("_ENABLE_WAVES");
                    mat.SetFloat("_EnableWaves", 0f);

                    for (int i = 1; i <= 4; i++)
                    {
                        mat.SetFloat($"_Wave{i}Amplitude", 0f);
                    }
                }
            }
        }
    }
}
#endif