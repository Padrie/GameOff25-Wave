using UnityEngine;

/// <summary>
/// Updates the global _UnscaledTimeGlobal shader property every frame.
/// This allows shaders to animate even when Time.timeScale = 0.
/// </summary>
public class GlimmerEffectUnscaledTime : MonoBehaviour
{
    private static readonly int UnscaledTimeGlobalID = Shader.PropertyToID("_UnscaledTimeGlobal");

    private void Update()
    {
        Shader.SetGlobalFloat(UnscaledTimeGlobalID, Time.unscaledTime);
    }
}
