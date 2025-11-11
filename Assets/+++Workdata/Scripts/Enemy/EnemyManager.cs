using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemyStats)), RequireComponent(typeof(NavMeshAgent))]
public class EnemyManager : MonoBehaviour
{
    [HideInInspector] NavMeshAgent agent;
    [HideInInspector] EnemyStats stats;
    [HideInInspector] Animator animator;

    [Header("Targets")]
    public GameObject playerTarget;
    public Transform lastPlayerPosTarget;
    public Transform soundTarget;

    StateMachine stateMachine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
        animator = GetComponent<Animator>();

        stateMachine = new StateMachine();

        //States
        var idleState = new IdleState(this, agent);
        var roamState = new RoamState(this, stats, agent);
        var playerChaseState = new ChasePlayerState(this, stats, agent);
        var soundChaseState = new ChaseSoundState(this, stats, agent);

        //State Transition
        stateMachine.AddTransition(idleState, roamState, HasNoTarget());

        stateMachine.AddTransition(idleState, playerChaseState, HasPlayerTarget());
        stateMachine.AddTransition(playerChaseState, idleState, HasPlayerNoTarget());

        stateMachine.AddTransition(idleState, soundChaseState, HasSoundTarget());
        stateMachine.AddTransition(soundChaseState, idleState, HasNoSoundTarget());

        Func<bool> HasNoTarget() => () => playerTarget == null && soundTarget == null && lastPlayerPosTarget == null;

        //State Transition checks
        Func<bool> HasPlayerTarget() => () => playerTarget != null;
        Func<bool> HasPlayerNoTarget() => () => playerTarget == null;

        Func<bool> HasSoundTarget() => () => soundTarget != null;
        Func<bool> HasNoSoundTarget() => () => soundTarget == null;

        stateMachine.SetState(idleState);
    }

    private void Update()
    {
        stateMachine.Tick();
    }
}
