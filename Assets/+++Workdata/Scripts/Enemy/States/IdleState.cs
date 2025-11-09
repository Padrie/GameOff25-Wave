using UnityEngine;

public class IdleState : IState
{
    private readonly EnemyManager enemyManager;

    public IdleState(EnemyManager enemyManager)
    {
        this.enemyManager = enemyManager;
    }

    public void OnEnter()
    {
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
