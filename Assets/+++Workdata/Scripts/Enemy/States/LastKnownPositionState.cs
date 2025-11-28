using UnityEngine;
using UnityEngine.AI;
using DG.Tweening;

public class LastKnownPositionState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;

    Vector3 lastPlayerPos = Vector3.zero;
    bool hasReachedPosition = false;
    bool isLookingAround = false;
    float lookAroundTimer = 0f;
    float lookAroundDuration = 4f;

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

        hasReachedPosition = false;
        isLookingAround = false;
        lookAroundTimer = 0f;
    }

    public void OnExit()
    {
        enemyManager.transform.DOKill();
        agent.isStopped = false;
    }

    public void Tick()
    {
        //Check if enemy has reached the last known position
        if (!hasReachedPosition && Vector3.Distance(enemyManager.transform.position, lastPlayerPos) < 1f)
        {
            hasReachedPosition = true;
            agent.isStopped = true;
            StartLookingAround();
        }

        if (isLookingAround)
        {
            lookAroundTimer += Time.deltaTime;

            if (lookAroundTimer >= lookAroundDuration)
            {
                //Finished, didn't find player
                agent.isStopped = false;
                enemyManager.lastPlayerPosTarget = null;
                enemyManager.lostPlayer = false;
            }
        }
    }

    private void StartLookingAround()
    {
        isLookingAround = true;

        //Create a natural looking around sequence
        Sequence lookSequence = DOTween.Sequence();

        //Look right (random amount between 30-120 degrees)
        float rightAngle = Random.Range(60f, 120f);
        lookSequence.Append(
            enemyManager.transform.DORotate(
                new Vector3(0, enemyManager.transform.eulerAngles.y + rightAngle, 0),
                Random.Range(0.4f, 0.7f),
                RotateMode.Fast
            ).SetEase(Ease.InOutSine)
        );

        //Pause briefly
        lookSequence.AppendInterval(Random.Range(0.2f, 0.4f));

        //Look left (past starting point, random amount between 100-180 degrees from right position)
        float leftAngle = Random.Range(100f, 180f);
        lookSequence.Append(
            enemyManager.transform.DORotate(
                new Vector3(0, enemyManager.transform.eulerAngles.y - leftAngle, 0),
                Random.Range(0.6f, 1f),
                RotateMode.Fast
            ).SetEase(Ease.InOutSine)
        );

        //Pause briefly
        lookSequence.AppendInterval(Random.Range(0.2f, 0.4f));

        //Look back to roughly forward (random final position within 45 degrees of start)
        float finalAngle = Random.Range(-45f, 45f);
        lookSequence.Append(
            enemyManager.transform.DORotate(
                new Vector3(0, enemyManager.transform.eulerAngles.y + leftAngle + finalAngle, 0),
                Random.Range(0.5f, 0.8f),
                RotateMode.Fast
            ).SetEase(Ease.InOutSine)
        );
    }
}