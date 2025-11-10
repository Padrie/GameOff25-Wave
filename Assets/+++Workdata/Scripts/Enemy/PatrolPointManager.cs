using EasyPeasyFirstPersonController;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;

public class PatrolPointManager : MonoBehaviour
{
    public List<PatrolPoint> patrolPoints;
    public PatrolPoint targetPatrolPoint;

    [SerializeField] Color finalPatrolPointColor = Color.blue;
    [SerializeField] Color goodPatrolPointColor = Color.green;
    [SerializeField] Color badPatrolPointColor = Color.yellow;

    List<PatrolPoint> finalPatrolPath;

    EnemyManager enemyManager;
    FirstPersonController player;

    Transform playerPos;

    private void Awake()
    {
        GameObject[] a = GameObject.FindGameObjectsWithTag("PatrolPoint");
        for (int i = 0; i < a.Length; i++)
            patrolPoints.Add(a[i].GetComponent<PatrolPoint>());

        enemyManager = GetComponent<EnemyManager>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<FirstPersonController>();
        patrolPoints.RemoveAll(x => !x);

        StartCoroutine(CalculatePath());
    }

    private void OnValidate()
    {
        patrolPoints.RemoveAll(x => !x);
    }

    private void Update()
    {
        playerPos = player.transform;
    }

    public void SelectRandomPatrolPoint()
    {
        targetPatrolPoint = patrolPoints[Random.Range(0, patrolPoints.Count)];
    }

    public IEnumerator CalculatePath()
    {
        int i = 0;
        int safeCheck = 0;
        bool calculatingPath = true;

        List<PatrolPoint> openPatrolPoints = new List<PatrolPoint>(patrolPoints);
        List<PatrolPoint> closedPatrolPoints = new List<PatrolPoint>();

        PatrolPoint startPoint = getNearestPatrolPointToEnemy().GetComponent<PatrolPoint>();
        PatrolPoint targetPoint = targetPatrolPoint;

        PatrolPoint currentPatrolPoint = getNearestPatrolPointToEnemy().GetComponent<PatrolPoint>();

        currentPatrolPoint.ChangeGizmoColor(goodPatrolPointColor);
        closedPatrolPoints.Add(currentPatrolPoint);
        openPatrolPoints.Remove(currentPatrolPoint);

        foreach (PatrolPoint p in openPatrolPoints)
        {
            if (p == targetPatrolPoint)
            {
                p.ChangeGizmoColor(finalPatrolPointColor);
                break;
            }
        } //Colors Final Patrol Point

        while (calculatingPath && safeCheck <= 1000)
        {
            if (openPatrolPoints.Count <= i)
                i = 0;



            i++;
            safeCheck++;
            yield return null;
        }

        yield return null;
    }

    public Vector3 getNextPatrolPointPosition()
    {
        return Vector3.zero;
    }

    public GameObject getNearestPatrolPointToEnemy()
    {
        var smallestDistance = patrolPoints[0].transform.position;
        GameObject smallestDistanceObject = null;

        for (int i = 1; i < patrolPoints.Count; i++)
        {
            if (Vector3.Distance(enemyManager.transform.position, patrolPoints[i].transform.position) < Vector3.Distance(enemyManager.transform.position, smallestDistance))
            {
                smallestDistance = patrolPoints[i].transform.position;
                smallestDistanceObject = patrolPoints[i].gameObject;
            }
        }

        return smallestDistanceObject;
    }
}
