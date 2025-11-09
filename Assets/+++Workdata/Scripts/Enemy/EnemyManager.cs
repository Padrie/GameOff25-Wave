using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemyStats)), RequireComponent(typeof(NavMeshAgent))]
public class EnemyManager : MonoBehaviour
{
    [HideInInspector] NavMeshAgent agent;
    [HideInInspector] EnemyStats stats;
    [HideInInspector] Animator animator;

    public GameObject target;

    StateMachine stateMachine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
        animator = GetComponent<Animator>();

        stateMachine = new StateMachine();

        //States
        var idle = new IdleState(this);
        var randomRoam = 2;
        var chase = new ChaseState(this, stats, agent);

        //State Transition
        stateMachine.AddTransition(idle, chase, HasTarget());
        stateMachine.AddTransition(chase, idle, HasNoTarget());

        //State Transition checks
        Func<bool> HasTarget() => () => target != null;
        Func<bool> HasNoTarget() => () => target == null;

        stateMachine.SetState(idle);
    }

    private void Update()
    {
        stateMachine.Tick();
    }
}
