using UnityEngine;
using UnityEngine.AI;

public class LastKnownPositionState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    float timeToMove = 0;

    public LastKnownPositionState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        Debug.Log("Entered LastKnownPositionState");

        agent.SetDestination(enemyManager.lastPlayerPosTarget.transform.position);
    }

    public void OnExit()
    {
        Debug.Log("Exited LastKnownPositionState");
    }

    public void Tick()
    {
        if (Vector3.Distance(enemyManager.transform.position, enemyManager.lastPlayerPosTarget.position) < 1f)
        {
            timeToMove += Time.deltaTime;

            if (timeToMove >= enemyStats.afterChaseWaitTime)
            {
                timeToMove = 0;

                enemyManager.lastPlayerPosTarget = null;
                enemyManager.lostPlayer = false;
            }
        }
    }
}
