using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemyStats))]
public class EnemyManager : MonoBehaviour
{
    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public EnemyStats stats;
    public Animator animator;

    [HideInInspector] public bool lostPlayer = false;

    public string currentState;

    [Header("Targets")]
    public Transform playerTarget;
    public Transform lastPlayerPosTarget;
    public Transform soundTarget;

    StateMachine stateMachine;
    float screamTime = 0f;

    public CircularWaveSpawner waveSpawner;

    private string currentAnimation = "";
    private const string ANIM_IDLE = "Idle";
    private const string ANIM_WALK = "Walk";
    private const string ANIM_CHASE = "Chase";
    private const string ANIM_SCREAM = "Scream";
    private const string ANIM_ATTACK = "Attack";

    //state references
    private IState idleState;
    private IState roamState;
    private IState screamState;
    private IState playerChaseState;
    private IState lastKnownPositionState;
    private IState soundChaseState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
        waveSpawner = FindFirstObjectByType<CircularWaveSpawner>();

        stateMachine = new StateMachine();

        screamTime = stats.screamCooldown;

        //States - Store references
        idleState = new IdleState(this, agent);
        roamState = new RoamState(this, stats, agent);
        screamState = new ScreamState(this, stats, agent);
        playerChaseState = new ChasePlayerState(this, stats, agent);
        lastKnownPositionState = new LastKnownPositionState(this, stats, agent);
        soundChaseState = new ChaseSoundState(this, stats, agent);

        //State Transition
        stateMachine.AddTransition(idleState, roamState, HasNoTarget());

        stateMachine.AddTransition(idleState, screamState, CanScream());
        stateMachine.AddTransition(roamState, screamState, CanScream());
        stateMachine.AddTransition(screamState, playerChaseState, CannotScream());
        stateMachine.AddTransition(screamState, idleState, CannotScream());

        stateMachine.AddTransition(idleState, playerChaseState, HasPlayerTarget());
        stateMachine.AddTransition(roamState, playerChaseState, HasPlayerTarget());
        stateMachine.AddTransition(playerChaseState, idleState, HasPlayerNoTarget());

        stateMachine.AddTransition(idleState, lastKnownPositionState, HasLastKnowPlayerPos());
        stateMachine.AddTransition(roamState, lastKnownPositionState, HasLastKnowPlayerPos());
        stateMachine.AddTransition(lastKnownPositionState, idleState, HasNoLastKnowPlayerPos());

        stateMachine.AddTransition(idleState, soundChaseState, HasSoundTarget());
        stateMachine.AddTransition(roamState, soundChaseState, HasSoundTarget());
        stateMachine.AddTransition(soundChaseState, idleState, HasNoSoundTarget());

        Func<bool> HasNoTarget() => () => playerTarget == null && soundTarget == null;

        Func<bool> CanScream() => () => CanScreamCheck() && playerTarget != null;
        Func<bool> CannotScream() => () => !CanScreamCheck();

        Func<bool> HasPlayerTarget() => () => playerTarget != null;
        Func<bool> HasPlayerNoTarget() => () => playerTarget == null;

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
            animator.SetFloat("Speed", agent.velocity.magnitude / agent.speed);

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (stateMachine.currentState == idleState)
        {
            PlayAnimation(ANIM_IDLE);
        }
        else if (stateMachine.currentState == roamState)
        {
            PlayAnimation(ANIM_WALK);
        }
        else if (stateMachine.currentState == playerChaseState)
        {
            PlayAnimation(ANIM_CHASE);
        }
        else if (stateMachine.currentState == screamState)
        {
            PlayAnimation(ANIM_SCREAM);
        }
        else if (stateMachine.currentState == lastKnownPositionState)
        {
            PlayAnimation(ANIM_WALK);
        }
        else if (stateMachine.currentState == soundChaseState)
        {
            PlayAnimation(ANIM_CHASE);
        }
    }

    private void PlayAnimation(string animationName)
    {
        if (currentAnimation != animationName && animator != null)
        {
            currentAnimation = animationName;
            animator.SetTrigger(animationName);
        }
    }

    public bool CanScreamCheck()
    {
        return screamTime >= stats.screamCooldown;
    }

    public void StartScreamCooldown()
    {
        waveSpawner.SpawnWaveAt(transform.position);
        StartCoroutine(Scream());
    }

    private IEnumerator Scream()
    {
        screamTime = 0;

        while (screamTime < stats.screamCooldown)
        {
            screamTime += Time.deltaTime;
            yield return null;
        }

        screamTime = stats.screamCooldown;
    }

    public void PlayIdle() => PlayAnimation(ANIM_IDLE);
    public void PlayWalk() => PlayAnimation(ANIM_WALK);
    public void PlayChase() => PlayAnimation(ANIM_CHASE);
    public void PlayScream() => PlayAnimation(ANIM_SCREAM);
    public void PlayAttack() => PlayAnimation(ANIM_ATTACK);
}