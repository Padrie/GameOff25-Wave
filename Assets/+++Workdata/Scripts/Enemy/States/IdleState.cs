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
        enemyManager.currentState = "Idle State";
        agent.SetDestination(enemyManager.transform.position);
        Debug.Log("Entered Idle State");
    }

    public void OnExit()
    {

    }

    public void Tick()
    {
    }
}
