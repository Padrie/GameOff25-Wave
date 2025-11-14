using UnityEngine;
using System.Collections.Generic;

public class CircularWaveSpawner : MonoBehaviour
{
    [Header("Material Setup")]
    [SerializeField] private Material[] waveMaterials;

    [Header("Player/Target Reference")]
    [Tooltip("The transform around which waves will spawn (usually the player)")]
    [SerializeField] private Transform playerTransform;

    [Header("Input Settings")]
    [SerializeField] private KeyCode spawnKey = KeyCode.X;
    [Tooltip("Spawn wave at player position or mouse position")]
    [SerializeField] private bool spawnAtPlayer = true;
    [SerializeField] private LayerMask raycastLayers = -1;

    [Header("Wave Properties")]
    [SerializeField] private float waveAmplitude = 1.5f;
    [SerializeField] private float propagationSpeed = 10f;
    [SerializeField] private float waveDuration = 3f;
    [SerializeField] private float wavelength = 4f;
    [SerializeField][Range(0f, 1f)] private float steepness = 0.5f;

    [Header("Safe Zone (Audio Wave Effect)")]
    [Tooltip("Radius around spawn point where ground is not deformed (like a speaker)")]
    [SerializeField] private float safeZoneRadius = 2f;
    [Tooltip("Show the safe zone in gizmos")]
    [SerializeField] private bool showSafeZone = true;

    [Header("Wave Behavior")]
    [SerializeField] private AnimationCurve amplitudeFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [Tooltip("How quickly the wave builds up at the start (seconds)")]
    [SerializeField] private float buildUpTime = 0.3f;
    [Tooltip("Curve controlling the build-up from 0 to full amplitude")]
    [SerializeField] private AnimationCurve buildUpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private int maxSimultaneousWaves = 4;

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private Color gizmoColor = new Color(0, 1, 1, 0.8f);

    private List<CircularWave> activeWaves = new List<CircularWave>();
    private List<Material> waveMaterialInstances = new List<Material>();
    private Camera cam;
    private bool wasPlaying = false;

    private class CircularWave
    {
        public Vector3 origin;
        public float birthTime;
        public float amplitude;
        public float speed;
        public float duration;
        public float wavelength;
        public float steepness;

        public float Age => Time.time - birthTime;
        public float Progress => Mathf.Clamp01(Age / duration);
        public float Radius => Age * speed;
        public bool IsAlive => Age < duration;
    }

    private void Awake()
    {
        cam = Camera.main;

        //Setup target renderers - automatically find all renderers with wave materials
        SetupTargetRenderers();

        //Auto-find player if not set
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                Debug.Log("Auto-found player: " + player.name);
            }
        }

        //Initialize all waves as disabled
        DisableAllWaves();

        Debug.Log($"✅ CircularWaveSpawner initialized with {waveMaterialInstances.Count} material instance(s). Press X to spawn.");
    }

    private void SetupTargetRenderers()
    {
        waveMaterialInstances.Clear();

        if (waveMaterials == null || waveMaterials.Length == 0)
        {
            Debug.LogError("⚠️ Wave Materials array is empty! Please assign at least one wave material in the inspector.");
            return;
        }

        List<Renderer> foundRenderers = new List<Renderer>();

        //Find all renderers in the scene
        Renderer[] allRenderers = FindObjectsOfType<Renderer>();

        //Find all renderers using any of the wave materials
        foreach (Renderer r in allRenderers)
        {
            if (r == null) continue;

            //Check all materials on this renderer
            Material[] materials = r.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                //Check if this material matches any of our wave materials
                foreach (Material waveMat in waveMaterials)
                {
                    if (waveMat == null) continue;

                    if (materials[i] == waveMat)
                    {
                        if (!foundRenderers.Contains(r))
                        {
                            foundRenderers.Add(r);
                            Debug.Log($"Found wave material '{waveMat.name}' on: {r.gameObject.name}");
                        }
                        break;
                    }
                }
            }
        }

        if (foundRenderers.Count == 0)
        {
            Debug.LogWarning($"⚠️ No renderers found using any of the {waveMaterials.Length} wave material(s). Make sure objects in your scene are using these materials.");
        }
        else
        {
            Debug.Log($"Auto-found {foundRenderers.Count} renderer(s) with wave materials");
        }

        //Create material instances for each renderer
        foreach (Renderer r in foundRenderers)
        {
            if (r == null) continue;

            //Find which material slots have wave materials
            Material[] materials = r.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null) continue;

                //Check if this material matches any of our wave materials
                foreach (Material waveMat in waveMaterials)
                {
                    if (waveMat == null) continue;

                    if (materials[i] == waveMat)
                    {
                        //Create instance of this specific material
                        Material instance = r.materials[i]; //This creates an instance automatically

                        if (!waveMaterialInstances.Contains(instance))
                        {
                            waveMaterialInstances.Add(instance);
                        }
                        break;
                    }
                }
            }
        }

        if (waveMaterialInstances.Count == 0)
        {
            Debug.LogWarning("⚠️ No target renderers found! Waves will not be visible.");
        }
        else
        {
            Debug.Log($"Created {waveMaterialInstances.Count} material instance(s) for wave rendering");
        }
    }

    private void OnEnable()
    {
        //Ensure waves are disabled when component is enabled
        if (waveMaterialInstances.Count > 0)
        {
            DisableAllWaves();
        }

#if UNITY_EDITOR
        //Subscribe to play mode state changes
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

    private void OnDisable()
    {
        //Clean up when disabled
        if (waveMaterialInstances.Count > 0)
        {
            DisableAllWaves();
        }

#if UNITY_EDITOR
        //Unsubscribe from play mode state changes
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    private void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
    {
        //Disable waves when entering edit mode
        if (state == UnityEditor.PlayModeStateChange.EnteredEditMode ||
            state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
        {
            DisableAllWaves();
        }
    }
#endif

    private void Update()
    {
        //Detect play mode changes
#if UNITY_EDITOR
        bool isPlaying = UnityEditor.EditorApplication.isPlaying;
        if (isPlaying && !wasPlaying)
        {
            //Just entered play mode - ensure waves are disabled
            DisableAllWaves();
        }
        else if (!isPlaying && wasPlaying)
        {
            //Just exited play mode - disable waves
            DisableAllWaves();
        }
        wasPlaying = isPlaying;

        //Only run wave logic in play mode
        if (!isPlaying) return;
#endif

        HandleInput();
        UpdateWaves();
        ApplyWavesToShader();
    }

    private void HandleInput()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            if (spawnAtPlayer && playerTransform != null)
            {
                SpawnWave(playerTransform.position);
            }
            else
            {
                SpawnWaveAtMousePosition();
            }
        }
    }

    private void SpawnWaveAtMousePosition()
    {
        if (cam == null)
        {
            Debug.LogError("Main camera not found!");
            return;
        }

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f, raycastLayers))
        {
            SpawnWave(hit.point);
        }
        else
        {
            Debug.Log("No surface hit by raycast. Try clicking on geometry.");
        }
    }

    private void SpawnWave(Vector3 worldPosition)
    {
        //Limit number of simultaneous waves
        if (activeWaves.Count >= maxSimultaneousWaves)
        {
            //Remove oldest wave
            activeWaves.RemoveAt(0);
            Debug.Log("Max waves reached. Removing oldest wave.");
        }

        //Enable wave keyword when first wave spawns
        if (activeWaves.Count == 0)
        {
            EnableWaveKeyword();
        }

        CircularWave wave = new CircularWave
        {
            origin = worldPosition,
            birthTime = Time.time,
            amplitude = waveAmplitude,
            speed = propagationSpeed,
            duration = waveDuration,
            wavelength = wavelength,
            steepness = steepness
        };

        activeWaves.Add(wave);

        Debug.Log($"🌊 Wave spawned at {worldPosition}. Will expand to ~{propagationSpeed * waveDuration:F1}m radius");
    }

    private void UpdateWaves()
    {
        //Remove expired waves
        activeWaves.RemoveAll(w => !w.IsAlive);

        //Disable wave keyword when no waves are active
        if (activeWaves.Count == 0)
        {
            DisableAllWaves();
        }
    }

    private void ApplyWavesToShader()
    {
        //Apply each active wave to a shader slot (max 4)
        for (int i = 0; i < 4; i++)
        {
            if (i < activeWaves.Count)
            {
                CircularWave wave = activeWaves[i];

                //Calculate build-up factor (0 to 1 during buildUpTime)
                float buildUpProgress = Mathf.Clamp01(wave.Age / buildUpTime);
                float buildUpFactor = buildUpCurve.Evaluate(buildUpProgress);

                //Calculate fade-out factor (1 to 0 during lifetime)
                float fadeOutFactor = amplitudeFalloff.Evaluate(wave.Progress);

                //Combine both: build up at start, fade out at end
                float amplitudeMultiplier = buildUpFactor * fadeOutFactor;
                float currentAmplitude = wave.amplitude * amplitudeMultiplier;

                SetShaderWave(i + 1, currentAmplitude, wave.wavelength, wave.speed, wave.steepness, wave.origin, wave.birthTime);
            }
            else
            {
                //Disable unused slot
                DisableShaderWave(i + 1);
            }
        }
    }

    private void SetShaderWave(int index, float amplitude, float wavelength, float speed, float steepness, Vector3 origin, float birthTime)
    {
        string prefix = $"_Wave{index}";
        Vector4 directionData = new Vector4(origin.x, origin.y, origin.z, safeZoneRadius);

        //Apply to all material instances
        foreach (Material mat in waveMaterialInstances)
        {
            if (mat == null) continue;

            mat.SetFloat(prefix + "Amplitude", amplitude);
            mat.SetFloat(prefix + "Wavelength", wavelength);
            mat.SetFloat(prefix + "Speed", speed);
            mat.SetFloat(prefix + "Steepness", steepness);
            mat.SetVector(prefix + "Direction", directionData);
            mat.SetFloat(prefix + "BirthTime", birthTime);
        }
    }

    private void DisableShaderWave(int index)
    {
        string prefix = $"_Wave{index}";

        //Apply to all material instances
        foreach (Material mat in waveMaterialInstances)
        {
            if (mat == null) continue;

            mat.SetFloat(prefix + "Amplitude", 0f);
            mat.SetFloat(prefix + "Wavelength", 10f);
            mat.SetFloat(prefix + "Speed", 1f);
            mat.SetFloat(prefix + "Steepness", 0f);
            mat.SetVector(prefix + "Direction", Vector4.zero);
            mat.SetFloat(prefix + "BirthTime", 0f);
        }
    }

    private void DisableAllWaves()
    {
        for (int i = 1; i <= 4; i++)
        {
            DisableShaderWave(i);
        }

        //Also disable the wave keyword to stop shader calculations completely
        foreach (Material mat in waveMaterialInstances)
        {
            if (mat == null) continue;
            mat.DisableKeyword("_ENABLE_WAVES");
            mat.SetFloat("_EnableWaves", 0f);
        }
    }

    private void EnableWaveKeyword()
    {
        //Enable wave keyword when waves are active
        foreach (Material mat in waveMaterialInstances)
        {
            if (mat == null) continue;
            mat.EnableKeyword("_ENABLE_WAVES");
            mat.SetFloat("_EnableWaves", 1f);
        }
    }

    //Public API
    /// <summary>
    /// Spawns a wave at the specified world position. Can be called from other scripts.
    /// </summary>
    /// <param name="position">The world position where the wave should spawn</param>
    public void SpawnWaveAt(Vector3 position)
    {
        SpawnWave(position);
    }

    public void SpawnWaveAtPlayer()
    {
        if (playerTransform != null)
        {
            SpawnWave(playerTransform.position);
        }
    }

    public void ClearAllWaves()
    {
        activeWaves.Clear();
        DisableAllWaves();
    }

    public void SetWaveAmplitude(float amplitude)
    {
        waveAmplitude = Mathf.Max(0f, amplitude);
    }

    public void SetPropagationSpeed(float speed)
    {
        propagationSpeed = Mathf.Max(0.1f, speed);
    }

    //Gizmos
    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || !Application.isPlaying) return;

        foreach (var wave in activeWaves)
        {
            float radius = wave.Radius;
            Vector3 origin = wave.origin;

            //Draw safe zone (no deformation)
            if (showSafeZone && safeZoneRadius > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
                DrawCircle(origin, safeZoneRadius, 32);

                Gizmos.color = new Color(1f, 0.5f, 0f, 0.1f);
                DrawFilledCircle(origin, safeZoneRadius, 16);
            }

            //Draw origin point
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(origin, 0.5f);

            //Draw expanding wave circle (starts at safe zone edge)
            float waveRadius = safeZoneRadius + radius;
            Gizmos.color = Color.Lerp(gizmoColor, Color.clear, wave.Progress);
            DrawCircle(origin, waveRadius, 64);

            //Draw wavelength indicator
            float innerRadius = Mathf.Max(safeZoneRadius, waveRadius - wave.wavelength);
            Gizmos.color = Color.Lerp(gizmoColor * 0.6f, Color.clear, wave.Progress);
            DrawCircle(origin, innerRadius, 48);
        }

        //Draw player position if set
        if (playerTransform != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(playerTransform.position, 1f);
        }
    }

    private void DrawCircle(Vector3 center, float radius, int segments)
    {
        if (radius <= 0) return;

        float angleStep = 360f / segments;
        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void DrawFilledCircle(Vector3 center, float radius, int segments)
    {
        if (radius <= 0) return;

        float angleStep = 360f / segments;

        for (int i = 0; i < segments; i++)
        {
            float angle1 = i * angleStep * Mathf.Deg2Rad;
            float angle2 = (i + 1) * angleStep * Mathf.Deg2Rad;

            Vector3 point1 = center + new Vector3(Mathf.Cos(angle1) * radius, 0, Mathf.Sin(angle1) * radius);
            Vector3 point2 = center + new Vector3(Mathf.Cos(angle2) * radius, 0, Mathf.Sin(angle2) * radius);

            Gizmos.DrawLine(center, point1);
            Gizmos.DrawLine(point1, point2);
        }
    }
}