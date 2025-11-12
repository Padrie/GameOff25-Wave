using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RoamState : IState
{
    private readonly EnemyManager enemyManager;
    private readonly EnemyStats enemyStats;
    private readonly NavMeshAgent agent;

    private List<Vector3> patrolPoints = new List<Vector3>();
    private int currentIndex = 0;
    private float arriveThreshold = 1.0f;

    public RoamState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        Debug.Log("Entered Roam State");

        //PatrolPointManager.instance.SelectRandomPatrolPoint();
        PatrolPointManager.instance.GetPath();

        patrolPoints = PatrolPointManager.instance.getAllCurrentPatrolPointPositions();

        if (patrolPoints.Count > 0)
        {
            currentIndex = 0;
            agent.SetDestination(patrolPoints[currentIndex]);
        }
    }

    public void OnExit()
    {
        Debug.Log("Exited Roam State");
        agent.ResetPath();
    }

    public void Tick()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        if (!agent.pathPending && agent.remainingDistance <= arriveThreshold)
        {
            currentIndex++;

            if (currentIndex >= patrolPoints.Count)
            {
                //PatrolPointManager.instance.SelectRandomPatrolPoint();
                PatrolPointManager.instance.GetPath();

                patrolPoints = PatrolPointManager.instance.getAllCurrentPatrolPointPositions();
                currentIndex = 0;
            }

            if (patrolPoints.Count > 0)
                agent.SetDestination(patrolPoints[currentIndex]);
        }
    }
}
