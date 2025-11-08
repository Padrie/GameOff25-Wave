using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[ExecuteAlways]
public class ShockwaveBurstSpawner : MonoBehaviour
{
    public Transform player;
    public KeyCode triggerKey = KeyCode.X;
    [Range(1, 16)] public int burstCount = 6;
    [Min(0.01f)] public float burstInterval = 0.08f;
    [Min(0.01f)] public float waveLifetime = 1.2f;
    [Range(1, 16)] public int maxWaves = 16;

    struct Wave { public Vector3 origin; public float startT; public float life; }
    readonly List<Wave> waves = new();

    Vector4[] waveData;
    float[] waveLife;

    Coroutine burstCo;

    static readonly int GW_WaveCountID = Shader.PropertyToID("_GW_WaveCount");
    static readonly int GW_WaveDataID = Shader.PropertyToID("_GW_WaveData");
    static readonly int GW_WaveLifeID = Shader.PropertyToID("_GW_WaveLife");

    void OnEnable() { EnsureBuffers(); UploadGlobals(); }

    void EnsureBuffers()
    {
        int n = Mathf.Clamp(maxWaves, 1, 16);
        if (waveData == null || waveData.Length != n) waveData = new Vector4[n];
        if (waveLife == null || waveLife.Length != n) waveLife = new float[n];
    }

    void Update()
    {
        if (!player) return;

        if (Input.GetKeyDown(triggerKey))
        {
            if (burstCo != null) StopCoroutine(burstCo);
            burstCo = StartCoroutine(EmitBurst(player.position));
        }

        CullExpired();
        UploadGlobals();
    }

    IEnumerator EmitBurst(Vector3 originAtPress)
    {
        float t0 = Time.time;
        for (int i = 0; i < burstCount; i++)
        {
            AddWave(originAtPress, t0 + i * burstInterval, waveLifetime);
            yield return new WaitForSeconds(burstInterval);
        }
    }

    void AddWave(Vector3 origin, float startTime, float life)
    {
        waves.Add(new Wave { origin = origin, startT = startTime, life = life });
        while (waves.Count > maxWaves) waves.RemoveAt(0);
    }

    void CullExpired()
    {
        float t = Time.time;
        for (int i = waves.Count - 1; i >= 0; i--)
            if (t - waves[i].startT > Mathf.Max(0.0001f, waves[i].life))
                waves.RemoveAt(i);
    }

    void UploadGlobals()
    {
        EnsureBuffers();
        for (int i = 0; i < waveData.Length; i++) { waveData[i] = Vector4.zero; waveLife[i] = 0f; }

        int count = Mathf.Min(waves.Count, waveData.Length);
        for (int i = 0; i < count; i++)
        {
            var w = waves[i];
            waveData[i] = new Vector4(w.origin.x, w.origin.y, w.origin.z, w.startT);
            waveLife[i] = w.life;
        }

        Shader.SetGlobalInt(GW_WaveCountID, count);
        Shader.SetGlobalVectorArray(GW_WaveDataID, waveData);
        Shader.SetGlobalFloatArray(GW_WaveLifeID, waveLife);
    }
}
