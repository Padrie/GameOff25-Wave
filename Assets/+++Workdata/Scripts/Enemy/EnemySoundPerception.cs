using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

public class EnemySoundPerception : MonoBehaviour
{
    [SerializeField] bool gizmosEnabled = true;
    [Header("Hearing Range")]
    public float faintSoundRadius = 4f;
    public float quietSoundRadius = 6f;
    public float normalSoundRadius = 8f;
    public float loudSoundRadius = 10f;
    public float veryLoudSoundRadius = 12f;

    EnemyManager enemyManager;
    NavMeshAgent agent;

    private void Awake()
    {
        enemyManager = GetComponent<EnemyManager>();
        agent = GetComponent<NavMeshAgent>();
    }

    private void OnEnable()
    {
        SoundManager.onSoundEmitted += CalculateSoundDistance;
    }

    private void OnDisable()
    {
        SoundManager.onSoundEmitted -= CalculateSoundDistance;
    }

    public void CalculateSoundDistance(Vector3 pos, SoundStrength s)
    {
        float soundStrength = SoundStrengthCalc(s);
        float dist = Vector3.Distance(transform.position, pos);

        if (dist > soundStrength) return;

        agent.SetDestination(pos);

        print($"The sound was {SoundStrengthString(s)}");
    }

    public float SoundStrengthCalc(SoundStrength s)
    {
        switch (s)
        {
            case SoundStrength.Faint: return faintSoundRadius;

            case SoundStrength.Quiet: return quietSoundRadius;

            case SoundStrength.Normal: return normalSoundRadius;

            case SoundStrength.Loud: return loudSoundRadius;

            case SoundStrength.VeryLoud: return veryLoudSoundRadius;

            default: return 0f;
        }
    }

    public string SoundStrengthString(SoundStrength s)
    {
        switch (s)
        {
            case SoundStrength.Faint: return "Faint";

            case SoundStrength.Quiet: return "Quiet";

            case SoundStrength.Normal: return "Normal";

            case SoundStrength.Loud: return "loud";

            case SoundStrength.VeryLoud: return "Very Loud";

            default: return "";
        }
    }

    private void OnDrawGizmos()
    {
        if (gizmosEnabled)
        {
            Handles.color = Color.green;
            Handles.DrawWireDisc(transform.position, Vector3.up, faintSoundRadius);
            Handles.color = Color.darkGreen;
            Handles.DrawWireDisc(transform.position, Vector3.up, quietSoundRadius);
            Handles.color = Color.yellow;
            Handles.DrawWireDisc(transform.position, Vector3.up, normalSoundRadius);
            Handles.color = Color.red;
            Handles.DrawWireDisc(transform.position, Vector3.up, loudSoundRadius);
            Handles.color = Color.darkRed;
            Handles.DrawWireDisc(transform.position, Vector3.up, veryLoudSoundRadius);
        }
    }
}
