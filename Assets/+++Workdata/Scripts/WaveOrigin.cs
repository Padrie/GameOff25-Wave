using UnityEngine;

public class WaveOriginShockwave : MonoBehaviour
{
    [Header("Assign the material used by your water/mesh")]
    public Material mat;

    [Header("Player to read position from")]
    public Transform player;

    [Header("Trigger a shockwave with this key (optional)")]
    public KeyCode triggerKey = KeyCode.X;

    [Header("Default lifetime for each shockwave (seconds)")]
    [Min(0.01f)]
    public float defaultLifetime = 1.0f;

    public void TriggerWave(float? lifetimeOverride = null)
    {
        if (!mat || !player) return;

        mat.SetVector("_WaveOriginWS", player.position);

        mat.SetFloat("_WaveStartTime", Time.time);

        mat.SetFloat("_WaveLifetime", lifetimeOverride.HasValue ? lifetimeOverride.Value : defaultLifetime);
    }

    private void Update()
    {
        if (!mat || !player) return;

        if (Input.GetKeyDown(KeyCode.X))
        {
            TriggerWave();
        }
    }
}
