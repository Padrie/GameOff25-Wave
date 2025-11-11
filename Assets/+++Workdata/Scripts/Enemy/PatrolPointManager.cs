using EasyPeasyFirstPersonController;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
public class PatrolPointManager : MonoBehaviour
{
    public List<PatrolPoint> patrolPoints;
    public PatrolPoint targetPatrolPoint;

    [Header("K Nearest")]
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] int maxNeighbors = 4;
    [SerializeField] float initialRadius = 8f;
    [SerializeField] float radiusStep = 8f;
    [SerializeField] float maxRadius = 64f;
    [SerializeField] bool useLineCast = true;

    [SerializeField] Color finalPatrolPointColor = Color.blue;
    [SerializeField] Color goodPatrolPointColor = Color.green;
    [SerializeField] Color badPatrolPointColor = Color.yellow;

    public List<PatrolPoint> finalPatrolPath;

    EnemyManager enemyManager;
    FirstPersonController player;

    Transform playerPos;

    public static PatrolPointManager instance;

    private void Awake()
    {
        instance = this;

        GameObject[] a = GameObject.FindGameObjectsWithTag("PatrolPoint");
        for (int i = 0; i < a.Length; i++)
            patrolPoints.Add(a[i].GetComponent<PatrolPoint>());

        enemyManager = GetComponent<EnemyManager>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<FirstPersonController>();

        patrolPoints.RemoveAll(x => !x);
    }

    private void Start()
    {
        GetPath();
    }

    private void OnValidate()
    {
        patrolPoints.RemoveAll(x => !x);
    }

    private void Update()
    {
        playerPos = player.transform;
        //GetPath();
    }

    public void GetPath()
    {
        ResetPatrolPoints();
        GetNeighbors();
        //StartCoroutine(CalculatePath());
        CalculatePath();
    }

    public void SelectRandomPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        float preferredRadius = 20f;
        float maxDistance = 50f;

        Vector3 playerPosition = player.transform.position;

        var nearbyPoints = patrolPoints
            .Where(p => Vector3.Distance(playerPosition, p.transform.position) <= preferredRadius)
            .ToList();

        if (nearbyPoints.Count > 0)
        {
            targetPatrolPoint = nearbyPoints[Random.Range(0, nearbyPoints.Count)];
        }
        else
        {
            var sorted = patrolPoints
                .OrderBy(p => Vector3.Distance(playerPosition, p.transform.position))
                .Take(5)
                .ToList();

            targetPatrolPoint = sorted[Random.Range(0, sorted.Count)];
        }

        targetPatrolPoint.ChangeGizmoColor(finalPatrolPointColor);
    }

    public void CalculatePath()
    {
        //yield return new WaitForSeconds(1f);
        //SelectRandomPatrolPoint();
        int safeCheck = 0;
        bool calculatingPath = true;

        List<PatrolPoint> openPatrolPoints = new List<PatrolPoint>();
        List<PatrolPoint> closedPatrolPoints = new List<PatrolPoint>();

        PatrolPoint startPoint = getNearestPatrolPoint(enemyManager.transform.position).GetComponent<PatrolPoint>();
        PatrolPoint targetPoint = targetPatrolPoint;

        startPoint.Setup(0, startPoint.transform.position, targetPoint.transform.position, null);
        startPoint.UpdateText();

        //move in While loop
        startPoint.ChangeGizmoColor(goodPatrolPointColor);
        targetPoint.ChangeGizmoColor(finalPatrolPointColor);

        openPatrolPoints.Add(startPoint);

        while (calculatingPath && safeCheck <= 10000)
        {
            if (openPatrolPoints.Count == 0)
            {
                Debug.LogWarning("Open list is empty");
                break;
            }

            PatrolPoint currentPatrolPoint = openPatrolPoints.OrderBy(p => p.GetF()).First();

            if (currentPatrolPoint == targetPoint)
            {
                finalPatrolPath = ReconstructPath(currentPatrolPoint);
                calculatingPath = false;
                break;
            }

            openPatrolPoints.Remove(currentPatrolPoint);
            closedPatrolPoints.Add(currentPatrolPoint);

            if (currentPatrolPoint.neighbors != null)
            {
                foreach (PatrolPoint neighbor in currentPatrolPoint.neighbors)
                {
                    if (neighbor == null) continue;
                    if (closedPatrolPoints.Contains(neighbor))
                    {
                        continue;
                    }

                    float tentativeG = currentPatrolPoint.GetG() + Vector3.Distance(currentPatrolPoint.transform.position, neighbor.transform.position);

                    bool setup = false;

                    if (!openPatrolPoints.Contains(neighbor))
                    {
                        setup = true;
                        openPatrolPoints.Add(neighbor);
                    }
                    else if (tentativeG >= neighbor.GetG())
                    {
                        setup = true;
                    }

                    if (setup)
                    {
                        neighbor.Setup(tentativeG, neighbor.transform.position, targetPatrolPoint.transform.position, currentPatrolPoint);
                        if (neighbor != targetPoint)
                            neighbor.ChangeGizmoColor(badPatrolPointColor);
                        neighbor.UpdateText();
                        //Debug.DrawLine(currentPatrolPoint.transform.position, neighbor.transform.position, Color.cyan, 1f);
                    }
                }
            }

            safeCheck++;
            //yield return null;
        }

        //yield return null;
    }

    public List<PatrolPoint> ReconstructPath(PatrolPoint current)
    {
        List<PatrolPoint> path = new List<PatrolPoint>();
        while (current != null)
        {
            path.Add(current);
            current = current.cameFrom;
        }

        path.Reverse();

        for (int i = 0; i < path.Count; i++)
        {
            if (path[i] != null)
            {
                if (path[i] != targetPatrolPoint)
                    path[i].ChangeGizmoColor(goodPatrolPointColor);
                //Debug.DrawLine(path[i].transform.position, path[i + 1].transform.position, Color.cyan, 10f);
            }
        }

        return path;
    }

    public void GetNeighbors()
    {
        int n = patrolPoints.Count;
        Vector3[] positions = new Vector3[n];

        for (int i = 0; i < n; i++) positions[i] = patrolPoints[i].transform.position;

        for (int i = 0; i < n; i++)
        {
            PatrolPoint a = patrolPoints[i];
            a.neighbors.Clear();

            float searchRadius = initialRadius;
            List<(PatrolPoint p, float dist)> validCandidates = new List<(PatrolPoint p, float dist)>();

            while (searchRadius <= maxRadius)
            {
                validCandidates.Clear();
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    float d = Vector3.Distance(positions[i], positions[j]);
                    if (d <= searchRadius)
                        validCandidates.Add((patrolPoints[j], d));
                }

                if (validCandidates.Count > 0)
                {
                    validCandidates = validCandidates.OrderBy(x => x.dist).ToList();

                    List<(PatrolPoint p, float dist)> visible = new List<(PatrolPoint p, float dist)>();
                    foreach (var c in validCandidates)
                    {
                        if (visible.Count >= maxNeighbors) break;

                        Vector3 from = positions[i] + Vector3.up * 0.25f;
                        Vector3 to = c.p.transform.position + Vector3.up * 0.25f;
                        Vector3 dir = to - from;
                        float dist = dir.magnitude;

                        bool blocked = false;
                        if (useLineCast)
                        {
                            blocked = Physics.Raycast(from, dir.normalized, dist, obstacleMask);
                        }

                        if (!blocked)
                            visible.Add(c);
                    }

                    if (visible.Count > 0)
                    {
                        foreach (var v in visible.Take(maxNeighbors))
                        {
                            a.neighbors.Add(v.p);
                        }
                        break;
                    }
                }

                searchRadius += radiusStep;
            }
        }

        for (int i = 0; i < n; i++)
        {
            PatrolPoint a = patrolPoints[i];
            foreach (var b in a.neighbors)
            {
                if (!b.neighbors.Contains(a)) b.neighbors.Add(a);
            }
        }
    }

    public void ResetPatrolPoints()
    {
        foreach (var p in patrolPoints)
            p.Reset();
        finalPatrolPath?.Clear();
    }

    public Vector3 getNextPatrolPointPosition(int index, out int i)
    {
        i = finalPatrolPath.Count;
        return finalPatrolPath[index].transform.position;
    }

    public GameObject getNearestPatrolPoint(Vector3 pos)
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return null;

        GameObject smallestDistanceObject = patrolPoints[0].gameObject;
        float smallestDistance = Vector3.Distance(pos, patrolPoints[0].transform.position);

        for (int i = 1; i < patrolPoints.Count; i++)
        {
            float d = Vector3.Distance(pos, patrolPoints[i].transform.position);
            if (d < smallestDistance)
            {
                smallestDistance = d;
                smallestDistanceObject = patrolPoints[i].gameObject;
            }
        }

        return smallestDistanceObject;
    }
}
