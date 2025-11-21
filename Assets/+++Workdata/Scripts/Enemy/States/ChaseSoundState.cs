using UnityEngine;
using UnityEngine.AI;

public class ChaseSoundState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    float timeToMove = 0;

    public ChaseSoundState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent agent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        this.agent = agent;
    }

    public void OnEnter()
    {
        enemyManager.currentState = "Chase Sound State";
        agent.speed = enemyStats.chaseSpeed;
    }

    public void OnExit()
    {
        agent.speed = enemyStats.walkSpeed;
    }

    public void Tick()
    {
        agent.SetDestination(enemyManager.soundTarget.position);
        if (Vector3.Distance(enemyManager.transform.position, enemyManager.soundTarget.position) < 1f)
        {
            timeToMove += Time.deltaTime;

            if (timeToMove >= enemyStats.afterChaseWaitTime)
            {
                timeToMove = 0;

                enemyManager.soundTarget = null;
            }
        }
    }
}
