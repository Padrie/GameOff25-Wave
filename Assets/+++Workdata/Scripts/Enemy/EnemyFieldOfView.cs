using UnityEngine;
using System.Collections;
using UnityEditor;

public class EnemyFieldOfView : MonoBehaviour
{
    [SerializeField] bool gizmosEnabled = true;
    [SerializeField] float viewRadius;
    [Range(0, 360)]
    [SerializeField] float viewAngle;

    [SerializeField] LayerMask targetMask;
    [SerializeField] LayerMask obstacleMask;

    [SerializeField] GameObject visibleTarget;
    GameObject playerTarget;
    EnemyManager enemyManager;

    bool hadVisibleTargetLastFrame = false;

    void Start()
    {
        StartCoroutine(FindTargetWithDelay(.1f));
        enemyManager = GetComponent<EnemyManager>();
        playerTarget = new GameObject("Player target");
    }


    IEnumerator FindTargetWithDelay(float delay)
    {
        while (true)
        {
            yield return new WaitForSeconds(delay);
            FindVisibleTarget();
        }
    }
    void FindVisibleTarget()
    {
        visibleTarget = null;
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, targetMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            GameObject target = targetsInViewRadius[i].gameObject;
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.transform.position);

                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    visibleTarget = target;
                    playerTarget.transform.position = target.transform.position;
                }
            }
        }
    }

    private void Update()
    {
        if (enemyManager == null)
            return;

        bool hasVisibleNow = visibleTarget != null;

        if (hadVisibleTargetLastFrame && !hasVisibleNow)
        {
            playerTarget.transform.position = enemyManager.playerTarget.position;
            enemyManager.lastPlayerPosTarget = playerTarget.transform;
        }

        if (hasVisibleNow)
            enemyManager.playerTarget = visibleTarget.transform;
        else
            enemyManager.playerTarget = null;

        hadVisibleTargetLastFrame = hasVisibleNow;
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }


#if (UNITY_EDITOR)
    private void OnDrawGizmosSelected()
    {
        if (gizmosEnabled)
        {
            Handles.color = Color.white;
            Handles.DrawWireArc(transform.position, Vector3.up, Vector3.forward, 360, viewRadius);
            Vector3 viewAngleA = DirFromAngle(-viewAngle / 2, false);
            Vector3 viewAngleB = DirFromAngle(viewAngle / 2, false);
            Handles.DrawLine(transform.position, transform.position + viewAngleA * viewRadius);
            Handles.DrawLine(transform.position, transform.position + viewAngleB * viewRadius);

            if (visibleTarget != null)
            {
                Handles.color = Color.red;
                Handles.DrawLine(transform.position, visibleTarget.transform.position);
            }
        }
    }
#endif
}