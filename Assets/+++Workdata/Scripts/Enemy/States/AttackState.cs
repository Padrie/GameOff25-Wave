using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Scripting;

public class AttackState : IState
{
    EnemyManager enemyManager;
    EnemyStats enemyStats;
    NavMeshAgent agent;
    Manager manager;

    private bool hasInitiatedGrab = false;
    private bool isGrabbing = false;
    private float attackTimer = 0f;
    private float attackDuration = 0f;

    private CharacterController playerController;
    private Rigidbody playerRigidbody;
    private MonoBehaviour firstPersonController;

    private Vector3 offsetFromTarget;
    private bool isFollowingTarget = false;
    private float lookAtWeight = 0f;
    private Transform lookAtTarget;

    public AttackState(EnemyManager enemyManager, EnemyStats enemyStats, NavMeshAgent navMeshAgent)
    {
        this.enemyManager = enemyManager;
        this.enemyStats = enemyStats;
        agent = navMeshAgent;
    }

    public void OnEnter()
    {
        enemyManager.currentState = "Attack State";
        agent.isStopped = true;
        hasInitiatedGrab = false;
        isGrabbing = false;
        attackTimer = 0f;
        isFollowingTarget = false;
        lookAtWeight = 0f;

        attackDuration = 3f;

        StartAttack();
    }

    public void OnExit()
    {
        agent.isStopped = false;
        isFollowingTarget = false;
        lookAtWeight = 0f;
        EndAttack();
    }

    public void Tick()
    {
        attackTimer += Time.deltaTime;

        if (enemyManager.playerTarget != null)
        {
            Vector3 direction = (enemyManager.playerTarget.transform.position - enemyManager.transform.position).normalized;
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            enemyManager.transform.rotation = Quaternion.Slerp(enemyManager.transform.rotation, lookRotation, Time.deltaTime * 5);
        }

        if (isFollowingTarget && enemyManager.playerCamera != null && enemyManager.attackParentTarget != null)
        {
            enemyManager.playerCamera.position = enemyManager.attackParentTarget.position + enemyManager.attackParentTarget.TransformDirection(offsetFromTarget);

            if (lookAtTarget != null && lookAtWeight > 0f)
            {
                Quaternion currentRotation = enemyManager.playerCamera.rotation;
                Vector3 directionToTarget = (lookAtTarget.position - enemyManager.playerCamera.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                enemyManager.playerCamera.rotation = Quaternion.Slerp(currentRotation, targetRotation, lookAtWeight);
            }
        }
    }

    private void StartAttack()
    {
        if (isGrabbing || hasInitiatedGrab)
            return;

        hasInitiatedGrab = true;

        if (enemyManager.playerCamera != null && enemyManager.attackParentTarget != null)
        {
            GrabCameraWithTween();
        }
    }

    private void GrabCameraWithTween()
    {
        if (enemyManager.playerCamera == null || enemyManager.attackParentTarget == null || isGrabbing)
            return;

        isGrabbing = true;

        if (playerController == null && enemyManager.playerTarget != null)
            playerController = enemyManager.playerTarget.GetComponent<CharacterController>();
        if (playerRigidbody == null && enemyManager.playerTarget != null)
            playerRigidbody = enemyManager.playerTarget.GetComponent<Rigidbody>();
        if (firstPersonController == null && enemyManager.playerTarget != null)
        {
            firstPersonController = enemyManager.playerTarget.GetComponent<MonoBehaviour>();
        }

        if (playerController != null)
            playerController.enabled = false;

        if (firstPersonController != null)
            firstPersonController.enabled = false;

        if (playerRigidbody != null)
        {
            playerRigidbody.isKinematic = true;
            playerRigidbody.linearVelocity = Vector3.zero;
        }

        lookAtTarget = enemyManager.enemyHeadTarget;

        DOVirtual.Float(0f, 1f, enemyManager.grabDuration, value =>
        {
            lookAtWeight = value;
        })
        .SetEase(Ease.Linear);

        Vector3 startPosition = enemyManager.playerCamera.position;
        Quaternion startRotation = enemyManager.playerCamera.rotation;

        enemyManager.playerCamera.DOKill();

        Sequence grabSequence = DOTween.Sequence();

        grabSequence.Append(
            DOVirtual.Float(0f, 1f, enemyManager.grabDuration, value =>
            {
                if (enemyManager.playerCamera != null && enemyManager.attackParentTarget != null)
                {
                    enemyManager.playerCamera.position = Vector3.Lerp(startPosition, enemyManager.attackParentTarget.position, 1);
                }
            })
            .SetEase(Ease.Linear)
        );

        grabSequence.OnComplete(() =>
        {
            if (enemyManager.playerCamera != null && enemyManager.attackParentTarget != null)
            {
                offsetFromTarget = enemyManager.attackParentTarget.InverseTransformDirection(enemyManager.playerCamera.position - enemyManager.attackParentTarget.position);
                isFollowingTarget = true;
            }
            isGrabbing = false;
        });

        grabSequence.OnKill(() =>
        {
            isGrabbing = false;
        });
    }

    private void EndAttack()
    {
        lookAtWeight = 0f;
        lookAtTarget = null;

        if (enemyManager.playerCamera != null)
        {
            enemyManager.playerCamera.DOKill();
        }

        if (playerController != null)
            playerController.enabled = true;

        if (firstPersonController != null)
            firstPersonController.enabled = true;

        if (playerRigidbody != null)
            playerRigidbody.isKinematic = false;

        isGrabbing = false;
        hasInitiatedGrab = false;

        manager = GameObject.FindFirstObjectByType<Manager>();

        manager.PlayerDeath();
    }

    public bool IsAttackFinished()
    {
        return attackTimer >= attackDuration;
    }
}