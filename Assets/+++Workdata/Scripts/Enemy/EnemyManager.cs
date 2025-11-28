using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemyStats))]
public class EnemyManager : MonoBehaviour
{
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyStats stats;
    public Animator animator;

    [HideInInspector] public bool lostPlayer = false;
    [HideInInspector] public bool isScreaming = false;
    [HideInInspector] public bool isAttacking = false;

    public string currentState;

    [Header("Targets")]
    public Transform playerTarget;
    public Transform lastPlayerPosTarget;
    public Transform soundTarget;

    [Header("Scream Sounds")]
    public AudioClip[] screamSounds;
    public AudioSource audioSource;

    StateMachine stateMachine;
    float screamTime = 0f;

    public CircularWaveSpawner waveSpawner;

    [Header("Attack Settings")]
    public Transform attackParentTarget;
    public Transform enemyHeadTarget;
    public Transform playerCamera;
    public float grabDuration = 0.5f;
    public float grabReachDistance = 3f;
    public AnimationCurve grabCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private IState previousState = null;

    private IState idleState;
    private IState roamState;
    private IState screamState;
    private IState attackState;
    private IState playerChaseState;
    private IState lastKnownPositionState;
    private IState soundChaseState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        stateMachine = new StateMachine();
        screamTime = stats.screamCooldown;

        idleState = new IdleState(this, agent);
        roamState = new RoamState(this, stats, agent);
        screamState = new ScreamState(this, stats, agent);
        attackState = new AttackState(this, stats, agent);
        playerChaseState = new ChasePlayerState(this, stats, agent);
        lastKnownPositionState = new LastKnownPositionState(this, stats, agent);
        soundChaseState = new ChaseSoundState(this, stats, agent);

        stateMachine.AddTransition(idleState, roamState, HasNoTarget());
        stateMachine.AddTransition(idleState, screamState, CanScream());
        stateMachine.AddTransition(roamState, screamState, CanScream());
        stateMachine.AddTransition(screamState, playerChaseState, ScreamFinished());
        stateMachine.AddTransition(idleState, playerChaseState, HasPlayerTarget());
        stateMachine.AddTransition(roamState, playerChaseState, HasPlayerTarget());
        stateMachine.AddTransition(playerChaseState, attackState, IsCloseToPlayer());
        stateMachine.AddTransition(playerChaseState, idleState, HasPlayerNoTarget());
        stateMachine.AddTransition(attackState, idleState, AttackFinished());
        stateMachine.AddTransition(idleState, lastKnownPositionState, HasLastKnowPlayerPos());
        stateMachine.AddTransition(roamState, lastKnownPositionState, HasLastKnowPlayerPos());
        stateMachine.AddTransition(lastKnownPositionState, idleState, HasNoLastKnowPlayerPos());
        stateMachine.AddTransition(idleState, soundChaseState, HasSoundTarget());
        stateMachine.AddTransition(roamState, soundChaseState, HasSoundTarget());
        stateMachine.AddTransition(soundChaseState, idleState, HasNoSoundTarget());

        Func<bool> HasNoTarget() => () => playerTarget == null;
        Func<bool> CanScream() => () => CanScreamCheck() && playerTarget != null;
        Func<bool> ScreamFinished() => () => !isScreaming;
        Func<bool> HasPlayerTarget() => () => playerTarget != null;
        Func<bool> HasPlayerNoTarget() => () => playerTarget == null;
        Func<bool> IsCloseToPlayer() => () => playerTarget != null && Vector3.Distance(transform.position, playerTarget.position) < grabReachDistance;
        Func<bool> AttackFinished() => () => ((AttackState)attackState).IsAttackFinished();
        Func<bool> HasLastKnowPlayerPos() => () => lastPlayerPosTarget != null && lostPlayer;
        Func<bool> HasNoLastKnowPlayerPos() => () => lastPlayerPosTarget == null || !lostPlayer;
        Func<bool> HasSoundTarget() => () => soundTarget != null;
        Func<bool> HasNoSoundTarget() => () => soundTarget == null || playerTarget != null;

        stateMachine.SetState(idleState);
    }

    private void Update()
    {
        stateMachine.Tick();

        if (animator != null)
            animator.SetFloat("Speed", agent.velocity.magnitude / Mathf.Max(agent.speed, 0.0001f));

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        //Detect state change
        if (previousState != stateMachine.currentState)
        {
            HandleStateExit(previousState);
            HandleStateEnter(stateMachine.currentState);

            previousState = stateMachine.currentState;
        }
    }
    private void HandleStateExit(IState exitingState)
    {
        if (exitingState == null) return;

        if (exitingState == screamState)
        {
            isScreaming = false;
        }
        else if (exitingState == attackState)
        {
            isAttacking = false;
        }
    }

    private void HandleStateEnter(IState enteringState)
    {
        if (enteringState == null) return;

        if (enteringState == screamState)
        {
            OnScreamStart();
            if (animator != null)
            {
                animator.SetTrigger("ScreamTrigger");
            }
        }
        else if (enteringState == attackState)
        {
            isAttacking = true;
            if (animator != null)
            {
                animator.SetTrigger("AttackTrigger");
            }
        }

        if (animator != null)
        {
            animator.SetInteger("State", GetStateId(enteringState));
        }
    }

    private int GetStateId(IState state)
    {
        if (state == idleState) return 0;
        if (state == roamState || state == lastKnownPositionState) return 1;
        if (state == playerChaseState || state == soundChaseState) return 2;
        if (state == screamState) return 3;
        if (state == attackState) return 4;

        return 0; //Default Idle
    }


    public bool CanScreamCheck()
    {
        return screamTime >= stats.screamCooldown;
    }

    public void OnScreamStart()
    {
        isScreaming = true;
        waveSpawner.SpawnWaveAt(transform.position);
        PlayRandomScreamSound();
    }

    public void EndScream()
    {
        isScreaming = false;
        StartCoroutine(ScreamCooldown());
    }

    private void PlayRandomScreamSound()
    {
        if (screamSounds != null && screamSounds.Length > 0 && audioSource != null)
        {
            int randomIndex = UnityEngine.Random.Range(0, screamSounds.Length);
            audioSource.volume = .2f;
            audioSource.PlayOneShot(screamSounds[randomIndex]);
        }
    }

    private IEnumerator ScreamCooldown()
    {
        screamTime = 0f;

        while (screamTime < stats.screamCooldown)
        {
            screamTime += Time.deltaTime;
            yield return null;
        }

        screamTime = stats.screamCooldown;
    }
}