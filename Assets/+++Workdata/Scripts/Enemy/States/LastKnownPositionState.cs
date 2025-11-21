using UnityEngine;
using UnityEngine.AI;

public class LastKnownPositionState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    float timeToMove = 0;

    Vector3 lastPlayerPos = Vector3.zero;

    public LastKnownPositionState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        enemyManager.currentState = "Last Known Position State";

        agent.SetDestination(enemyManager.lastPlayerPosTarget.transform.position);
        lastPlayerPos = enemyManager.lastPlayerPosTarget.transform.position;
    }

    public void OnExit()
    {

    }

    public void Tick()
    {
        if (Vector3.Distance(enemyManager.transform.position, lastPlayerPos) < 1f)
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
