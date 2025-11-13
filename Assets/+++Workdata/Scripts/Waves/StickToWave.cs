using UnityEngine;
using System.Collections.Generic;

public class StickToWave : MonoBehaviour
{
    private CircularWaveSpawner waveSpawner;
    [SerializeField][Range(0f, 1.5f)] private float waveInfluence = 1.0f;
    [SerializeField] private bool smoothDisplacement = true;
    [SerializeField][Range(1f, 30f)] private float smoothSpeed = 20f;
    [SerializeField] private float groundOffset = 0f;
    [SerializeField] private bool showDebugInfo = false;

    private Vector3 basePosition;
    private Vector3 currentWaveOffset;
    private Vector3 targetWaveOffset;
    private Material cachedWaveMaterial;
    private List<Material> waveMaterialInstances;

    private const float PI = 3.14159265f;
    private const float TWO_PI = 6.28318531f;
    private const float GRAVITY = 9.8f;
    private const float MIN_AMPLITUDE = 0.001f;
    private const float MIN_WAVELENGTH = 0.1f;
    private const float MIN_DISTANCE = 0.001f;

    private static readonly int[] WaveIndices = { 1, 2, 3, 4 };
    private static readonly string[] PropertyNames = { "Amplitude", "Wavelength", "Speed", "Steepness", "Direction", "BirthTime" };

    private void Start()
    {
        waveSpawner = FindFirstObjectByType<CircularWaveSpawner>();


        basePosition = transform.position;
        CacheWaveMaterial();
    }

    private void Update()
    {
        if (cachedWaveMaterial == null) return;

        basePosition = transform.position - currentWaveOffset;
        targetWaveOffset = CalculateWaveDisplacement(basePosition + Vector3.up * groundOffset);

        currentWaveOffset = smoothDisplacement
            ? Vector3.Lerp(currentWaveOffset, targetWaveOffset, Time.deltaTime * smoothSpeed)
            : targetWaveOffset;

        transform.position = basePosition + currentWaveOffset;
    }

    private Vector3 CalculateWaveDisplacement(Vector3 samplePos)
    {
        Vector3 totalOffset = Vector3.zero;
        float currentTime = Time.time;

        for (int i = 0; i < 4; i++)
        {
            totalOffset += CalculateSingleWave(samplePos, WaveIndices[i], currentTime);
        }

        return totalOffset * waveInfluence;
    }

    private Vector3 CalculateSingleWave(Vector3 position, int waveIndex, float currentTime)
    {
        string prefix = $"_Wave{waveIndex}";

        float amplitude = cachedWaveMaterial.GetFloat(prefix + PropertyNames[0]);
        if (amplitude < MIN_AMPLITUDE) return Vector3.zero;

        float wavelength = cachedWaveMaterial.GetFloat(prefix + PropertyNames[1]);
        float speed = cachedWaveMaterial.GetFloat(prefix + PropertyNames[2]);
        float steepness = cachedWaveMaterial.GetFloat(prefix + PropertyNames[3]);
        Vector4 directionData = cachedWaveMaterial.GetVector(prefix + PropertyNames[4]);
        float birthTime = cachedWaveMaterial.GetFloat(prefix + PropertyNames[5]);

        float time = currentTime - birthTime;
        if (time < 0) return Vector3.zero;

        float dx = position.x - directionData.x;
        float dz = position.z - directionData.z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);

        float innerRadius = directionData.w;
        if (distance < innerRadius) return Vector3.zero;

        float effectiveDistance = distance - innerRadius;
        float k = TWO_PI / Mathf.Max(wavelength, MIN_WAVELENGTH);
        float c = Mathf.Sqrt(GRAVITY / k);
        float waveFrontDistance = c * speed * time;
        float trailLength = wavelength * 2.5f;
        float distanceFromFront = waveFrontDistance - effectiveDistance;

        if (distanceFromFront < -wavelength * 0.5f || distanceFromFront > trailLength)
            return Vector3.zero;

        float invDistance = distance > MIN_DISTANCE ? 1f / distance : 0f;
        float dirX = dx * invDistance;
        float dirZ = dz * invDistance;

        float phase = k * effectiveDistance - k * c * speed * time;

        float frontFade = SmoothStep(-wavelength * 0.5f, wavelength * 0.2f, distanceFromFront);
        float trailFade = SmoothStep(trailLength, trailLength * 0.5f, distanceFromFront);
        float safeZoneFade = SmoothStep(0, wavelength * 0.5f, effectiveDistance);
        float fadeFactor = frontFade * trailFade * safeZoneFade;

        float Q = steepness / k;
        float cosPhase = Mathf.Cos(phase);
        float sinPhase = Mathf.Sin(phase);
        float effectiveAmplitude = amplitude * fadeFactor;

        float horizontalDisp = Q * effectiveAmplitude * cosPhase;

        return new Vector3(
            dirX * horizontalDisp,
            effectiveAmplitude * sinPhase,
            dirZ * horizontalDisp
        );
    }

    private float SmoothStep(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3.0f - 2.0f * t);
    }

    private void CacheWaveMaterial()
    {
        if (waveSpawner == null) return;

        var field = typeof(CircularWaveSpawner).GetField("waveMaterialInstances",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            waveMaterialInstances = field.GetValue(waveSpawner) as List<Material>;
            if (waveMaterialInstances != null && waveMaterialInstances.Count > 0)
                cachedWaveMaterial = waveMaterialInstances[0];
        }
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.25f);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(basePosition, transform.position);

    }

    public Vector3 GetCurrentWaveOffset() => currentWaveOffset;
    public Vector3 GetBasePosition() => basePosition;
}