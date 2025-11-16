using UnityEngine;
using UnityEngine.AI;
public class ChasePlayerState : IState
{
    private readonly EnemyManager enemyManager;
    private readonly EnemyStats enemyStats;
    private readonly NavMeshAgent agent;

    public ChasePlayerState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent agent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        this.agent = agent;
    }

    public void OnEnter()
    {
        Debug.Log("Entered Chase State");
        enemyManager.lostPlayer = false;
        agent.speed = enemyStats.chaseSpeed;
    }

    public void OnExit()
    {
        enemyManager.lostPlayer = true;
        agent.speed = enemyStats.walkSpeed;
        Debug.Log("Exited Chase State");
    }

    public void Tick()
    {
        agent.SetDestination(enemyManager.playerTarget.transform.position);
    }
}
