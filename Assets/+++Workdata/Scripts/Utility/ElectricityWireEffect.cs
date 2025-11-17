using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ElectricityWireEffect : MonoBehaviour
{
    [Header("References")]
    [Tooltip("All UtilityPoleWire components (auto-found in children)")]
    private UtilityPoleWire[] wireScripts;

    [Header("Electricity Effect Settings")]
    [Tooltip("How often electricity shoots along the wire (in seconds)")]
    public float electricityInterval = 3f;

    [Tooltip("Random variation in interval (±seconds)")]
    public float intervalVariation = 1f;

    [Tooltip("Speed of electricity traveling along wire")]
    public float electricitySpeed = 10f;

    [Tooltip("Enable automatic electricity effects")]
    public bool autoTrigger = true;

    [Header("Particle System")]
    [Tooltip("Particle system prefab for electricity effect")]
    public ParticleSystem electricityParticlesPrefab;

    [Tooltip("Particle emission rate")]
    public int particleEmissionRate = 50;

    [Tooltip("Particle lifetime")]
    public float particleLifetime = 0.5f;

    [Tooltip("Particle size")]
    public float particleSize = 0.1f;

    [Tooltip("Particle color")]
    public Color particleColor = new Color(0.3f, 0.6f, 1f, 1f);

    [Header("Point Light")]
    [Tooltip("Enable point light effect")]
    public bool usePointLight = true;

    [Tooltip("Light color")]
    public Color lightColor = new Color(0.5f, 0.8f, 1f, 1f);

    [Tooltip("Light intensity")]
    public float lightIntensity = 2f;

    [Tooltip("Light range")]
    public float lightRange = 5f;

    [Header("Audio (Optional)")]
    [Tooltip("Sound to play when electricity shoots")]
    public AudioClip electricitySound;

    [Tooltip("Volume of the sound")]
    [Range(0f, 1f)]
    public float soundVolume = 0.5f;

    private GameObject effectObject;
    private ParticleSystem activeParticles;
    private Light activeLight;
    private AudioSource audioSource;
    private List<bool> wireRunningStates = new List<bool>();
    private float nextTriggerTime;

    private void Awake()
    {
        wireScripts = GetComponentsInChildren<UtilityPoleWire>();

        if (wireScripts.Length == 0)
        {
            Debug.LogWarning("No UtilityPoleWire components found in children!");
        }

        for (int i = 0; i < wireScripts.Length; i++)
        {
            wireRunningStates.Add(false);
        }

        if (electricitySound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = electricitySound;
            audioSource.volume = soundVolume;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
        }
    }

    private void Start()
    {
        if (autoTrigger)
        {
            ScheduleNextTrigger();
        }
    }

    private void Update()
    {
        if (autoTrigger && Time.time >= nextTriggerTime && wireScripts.Length > 0)
        {
            //Pick a random wire that's not currently running
            List<int> availableWires = new List<int>();
            for (int i = 0; i < wireScripts.Length; i++)
            {
                if (!wireRunningStates[i])
                {
                    availableWires.Add(i);
                }
            }

            if (availableWires.Count > 0)
            {
                int randomWireIndex = availableWires[Random.Range(0, availableWires.Count)];
                TriggerElectricity(randomWireIndex);
            }

            ScheduleNextTrigger();
        }
    }

    private void ScheduleNextTrigger()
    {
        float randomVariation = Random.Range(-intervalVariation, intervalVariation);
        nextTriggerTime = Time.time + electricityInterval + randomVariation;
    }

    /// <summary>
    /// Manually trigger an electricity effect on a specific wire
    /// </summary>
    public void TriggerElectricity(int wireIndex)
    {
        if (wireScripts == null || wireScripts.Length == 0)
        {
            Debug.LogWarning("No UtilityPoleWire components found!");
            return;
        }

        if (wireIndex < 0 || wireIndex >= wireScripts.Length)
        {
            Debug.LogWarning($"Wire index {wireIndex} out of range!");
            return;
        }

        UtilityPoleWire wire = wireScripts[wireIndex];

        if (wire == null || wire.targetPole == null)
        {
            Debug.LogWarning($"Wire {wireIndex} not properly set up!");
            return;
        }

        if (wireRunningStates[wireIndex])
        {
            return; // Already running on this wire
        }

        StartCoroutine(ShootElectricity(wire, wireIndex));
    }

    /// <summary>
    /// Trigger electricity on a random wire
    /// </summary>
    [ContextMenu("Trigger Electricity (Random Wire)")]
    public void TriggerElectricity()
    {
        if (wireScripts == null || wireScripts.Length == 0)
        {
            Debug.LogWarning("No UtilityPoleWire components found!");
            return;
        }

        // Pick a random wire
        int randomIndex = Random.Range(0, wireScripts.Length);
        TriggerElectricity(randomIndex);
    }

    /// <summary>
    /// Trigger electricity on all wires simultaneously
    /// </summary>
    [ContextMenu("Trigger Electricity (All Wires)")]
    public void TriggerElectricityAllWires()
    {
        for (int i = 0; i < wireScripts.Length; i++)
        {
            TriggerElectricity(i);
        }
    }

    private IEnumerator ShootElectricity(UtilityPoleWire wire, int wireIndex)
    {
        wireRunningStates[wireIndex] = true;

        if (audioSource != null && electricitySound != null)
        {
            audioSource.Play();
        }

        GameObject effectObj = new GameObject($"ElectricityEffect_Wire{wireIndex}");

        //Setup particle system
        ParticleSystem particles;
        if (electricityParticlesPrefab != null)
        {
            particles = Instantiate(electricityParticlesPrefab, effectObj.transform);
        }
        else
        {
            particles = CreateDefaultParticleSystem(effectObj);
        }

        var emission = particles.emission;
        emission.rateOverTime = particleEmissionRate;

        var main = particles.main;
        main.startLifetime = particleLifetime;
        main.startSize = particleSize;
        main.startColor = particleColor;

        Light lightComponent = null;
        if (usePointLight)
        {
            GameObject lightObj = new GameObject("ElectricityLight");
            lightObj.transform.SetParent(effectObj.transform);
            lightComponent = lightObj.AddComponent<Light>();
            lightComponent.type = LightType.Point;
            lightComponent.color = lightColor;
            lightComponent.intensity = lightIntensity;
            lightComponent.range = lightRange;
            lightComponent.shadows = LightShadows.None;
        }

        Vector3 startPos = wire.transform.position;
        Vector3 endPos = wire.targetPole.position;

        float totalDistance = Vector3.Distance(startPos, endPos);
        float duration = totalDistance / electricitySpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector3 currentPos = CalculateWirePosition(wire, t);
            effectObj.transform.position = currentPos;

            if (lightComponent != null)
            {
                lightComponent.intensity = lightIntensity * (0.8f + Random.Range(0f, 0.4f));
            }

            yield return null;
        }

        // Cleanup
        if (particles != null)
        {
            var emissionModule = particles.emission;
            emissionModule.enabled = false;
            Destroy(particles.gameObject, particleLifetime);
        }

        if (lightComponent != null)
        {
            Destroy(lightComponent.gameObject);
        }

        Destroy(effectObj, particleLifetime + 0.5f);

        wireRunningStates[wireIndex] = false;
    }

    private Vector3 CalculateWirePosition(UtilityPoleWire wire, float t)
    {
        Vector3 startPos = wire.transform.position;
        Vector3 endPos = wire.targetPole.position;

        // Linear interpolation
        Vector3 position = Vector3.Lerp(startPos, endPos, t);

        // Add sag if enabled
        if (wire.useSag)
        {
            float sag = wire.sagAmount * (1f - Mathf.Pow(2f * t - 1f, 2f));
            position.y -= sag;
        }

        return position;
    }

    private ParticleSystem CreateDefaultParticleSystem(GameObject parent)
    {
        GameObject psObj = new GameObject("DefaultElectricityParticles");
        psObj.transform.SetParent(parent.transform);
        psObj.transform.localPosition = Vector3.zero;

        ParticleSystem ps = psObj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.startSpeed = 0.5f;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.startLifetime = particleLifetime;
        main.maxParticles = 100;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = particleEmissionRate;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(particleColor, 0f), new GradientColorKey(particleColor, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        colorOverLifetime.color = gradient;

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.SetColor("_Color", particleColor);

        return ps;
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        for (int i = 0; i < wireRunningStates.Count; i++)
        {
            wireRunningStates[i] = false;
        }
    }
}