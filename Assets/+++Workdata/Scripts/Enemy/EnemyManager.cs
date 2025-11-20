using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemyStats)), RequireComponent(typeof(NavMeshAgent))]
public class EnemyManager : MonoBehaviour
{
    [HideInInspector] NavMeshAgent agent;
    [HideInInspector] EnemyStats stats;
    [HideInInspector] Animator animator;

    [HideInInspector] public bool lostPlayer = false;

    [Header("Targets")]
    public Transform playerTarget;
    public Transform lastPlayerPosTarget;
    public Transform soundTarget;

    StateMachine stateMachine;
    float screamTime = 0f;


    public CircularWaveSpawner waveSpawner;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
        animator = GetComponent<Animator>();
        waveSpawner = FindFirstObjectByType<CircularWaveSpawner>();

        stateMachine = new StateMachine();

        screamTime = stats.screamCooldown;

        //States
        var idleState = new IdleState(this, agent);
        var roamState = new RoamState(this, stats, agent);
        var screamState = new ScreamState(this, stats, agent);
        var playerChaseState = new ChasePlayerState(this, stats, agent);
        var lastKnownPositionState = new LastKnownPositionState(this, stats, agent);
        var soundChaseState = new ChaseSoundState(this, stats, agent);

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

        //State Transition checks
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
    }

    //private bool EvaluateTarget()
    //{

    //}

    public bool CanScreamCheck()
    {
        if (screamTime >= stats.screamCooldown)
            return true;
        else
            return false;
    }

    public void StartScreamCooldown()
    {
        waveSpawner.SpawnWaveAt(transform.position);
        StartCoroutine(Scream());
    }

    private IEnumerator Scream()
    {
        bool stopLoop = false;
        screamTime = 0;

        while (!stopLoop)
        {
            screamTime += Time.deltaTime;

            if (screamTime > stats.screamCooldown)
            {
                screamTime = stats.screamCooldown;
                stopLoop = true;
            }

            yield return null;
        }
        yield return null;
    }
}
