using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class ScreamState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    public ScreamState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        //Play Scream sound
        //enemyManager.PlaySound(enemyStats.monsterScream[Random.Range(0, enemyStats.monsterScream.Length)]);
        enemyManager.currentState = "Scream State";

        enemyManager.StartCoroutine(StartScream());
    }

    public void OnExit()
    {

    }

    IEnumerator StartScream()
    {
        //Determine length of sound clip
        yield return new WaitForSeconds(2f);
        enemyManager.StartScreamCooldown();
        yield return null;
    }

    public void Tick()
    {
        if (enemyManager.playerTarget != null)
        {
            Vector3 direction = (enemyManager.playerTarget.transform.position - enemyManager.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            enemyManager.transform.rotation = Quaternion.Slerp(enemyManager.transform.rotation, lookRotation, Time.deltaTime * 5);
        }
    }
}
