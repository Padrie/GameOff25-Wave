using UnityEngine;
using UnityEngine.AI;

public class LastKnownPositionState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    GameObject playerTransform;

    public LastKnownPositionState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        Debug.Log("Entered LastKnownPositionState");

        if (playerTransform == null)
            playerTransform = new GameObject();

        playerTransform.transform.position = enemyManager.lastPlayerPosTarget.position;

        agent.SetDestination(playerTransform.transform.position);
    }

    public void OnExit()
    {
        Debug.Log("Exited LastKnownPositionState");
    }

    public void Tick()
    {
        if (Vector3.Distance(enemyManager.transform.position, enemyManager.lastPlayerPosTarget.position) < 0.1f)
        {
            enemyManager.lastPlayerPosTarget = null;
        }
    }
}
