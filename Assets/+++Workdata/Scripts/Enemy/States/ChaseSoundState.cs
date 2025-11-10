using UnityEngine;
using UnityEngine.AI;

public class ChaseSoundState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    public ChaseSoundState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent agent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        this.agent = agent;
    }

    public void OnEnter()
    {
        Debug.Log("Entered Sound Chase State");
        agent.speed = enemyStats.chaseSpeed;
    }

    public void OnExit()
    {
        Debug.Log("Exited Sound Chase State");
        agent.speed = enemyStats.walkSpeed;
    }

    public void Tick()
    {
        agent.SetDestination(enemyManager.soundTarget.position);
        if (Vector3.Distance(enemyManager.transform.position, enemyManager.soundTarget.position) < 0.1f)
        {
            enemyManager.soundTarget = null;
        }
    }
}
