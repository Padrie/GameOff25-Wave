using UnityEngine;
using UnityEngine.AI;

public class RoamState : IState
{
    private readonly EnemyManager enemyManager;
    private readonly EnemyStats enemyStats;
    private readonly NavMeshAgent agent;

    public RoamState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        Debug.Log("Entered Roam State");
    }

    public void OnExit()
    {
        Debug.Log("Exited Roam State");
    }

    int sizeOfPatrolList = 0;
    int index = 0;

    public void Tick()
    {
        var posOfPatrolPoint = PatrolPointManager.instance.getNextPatrolPointPosition(index, out sizeOfPatrolList);

        agent.SetDestination(posOfPatrolPoint);

        if (Vector3.Distance(enemyManager.transform.position, posOfPatrolPoint) <= 0.1f)
        {
            index++;

            if (index >= sizeOfPatrolList)
                index = 0;
        }
    }
}
