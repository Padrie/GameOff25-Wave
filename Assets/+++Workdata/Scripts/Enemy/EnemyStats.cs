using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float chaseSpeed = 5f;
    
    [Header("Roam State")]
    public float roamWaitTime = 2f;
    [Header("Chase State")]
    public float screamCooldown = 120f;
    public float afterChaseWaitTime = 2f;
    [Header("Enemy Sounds")]
    public AudioClip[] monsterFootSteps;
    public AudioClip[] monsterAmbience;
    public AudioClip[] chaseFootSteps;
    public AudioClip[] monsterScream;
}
