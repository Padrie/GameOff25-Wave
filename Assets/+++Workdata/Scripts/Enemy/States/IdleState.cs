using UnityEngine;
using UnityEngine.AI;

public class IdleState : IState
{
    private readonly EnemyManager enemyManager;
    private readonly NavMeshAgent agent;

    public IdleState(EnemyManager enemyManager, NavMeshAgent agent)
    {
        this.enemyManager = enemyManager;
        this.agent = agent;
    }

    public void OnEnter()
    {
        agent.SetDestination(enemyManager.transform.position);
        Debug.Log("Entered Idle State");
    }

    public void OnExit()
    {
        Debug.Log("Exited Idle State");

    }

    public void Tick()
    {
    }
}
