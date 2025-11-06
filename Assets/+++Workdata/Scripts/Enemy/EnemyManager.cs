using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent)), RequireComponent(typeof(EnemyStats))]
public class EnemyManager : MonoBehaviour
{
    [HideInInspector] NavMeshAgent agent;
    [HideInInspector] EnemyStats stats;
    [HideInInspector] Animator animator;
    StateMachine stateMachine;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        stats = GetComponent<EnemyStats>();
        animator = GetComponent<Animator>();

        stateMachine = new StateMachine();


    }

    private void Update()
    {
        stateMachine.Tick();
    }
}
