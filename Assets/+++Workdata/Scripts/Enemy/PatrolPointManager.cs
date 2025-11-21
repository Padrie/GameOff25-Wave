using EasyPeasyFirstPersonController;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Unity.Burst;
using Unity.VisualScripting;

public class PatrolPointManager : MonoBehaviour
{
    static readonly Vector3 RAYCAST_OFFSET = new Vector3(0, 0.25f, 0);

    [SerializeField] List<PatrolPoint> patrolPoints;
    [SerializeField] PatrolPoint targetPatrolPoint;
    [SerializeField] float selectRandomPointAroundPlayer = 20f;
    [SerializeField] bool drawGizmos = true;

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

    [SerializeField] List<PatrolPoint> finalPatrolPath;

    HashSet<PatrolPoint> touchedPatrolPoints = new HashSet<PatrolPoint>();

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
        if (!drawGizmos)
        {
            foreach (PatrolPoint p in patrolPoints)
            {
                p.drawGizmos = false;
            }
        }

        GetNeighbors();
        SelectRandomPatrolPoint();
        GetPath();
    }

    private void OnValidate()
    {
        patrolPoints.RemoveAll(x => !x);
    }

    private void Update()
    {
        playerPos = player.transform;
    }

    public void GetPath()
    {
        ResetPatrolPoints();
        //StartCoroutine(CalculatePath());
        CalculatePath();
    }

    public void SelectRandomPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        Vector3 playerPosition = player.transform.position;

        var nearbyPoints = patrolPoints
            .Where(p => (playerPosition - p.transform.position).sqrMagnitude <= selectRandomPointAroundPlayer * selectRandomPointAroundPlayer)
            .ToList();

        if (nearbyPoints.Count > 0)
        {
            targetPatrolPoint = nearbyPoints[Random.Range(0, nearbyPoints.Count)];
        }
        else
        {
            var sorted = patrolPoints
                .OrderBy(p => (playerPosition - p.transform.position).sqrMagnitude)
                .Take(5)
                .ToList();

            targetPatrolPoint = sorted[Random.Range(0, sorted.Count)];
        }

        if (drawGizmos)
            targetPatrolPoint.ChangeGizmoColor(finalPatrolPointColor);
    }

    public void CalculatePath()
    {
        bool calculatingPath = true;

        PriorityQueue<PatrolPoint> openPatrolPoints = new PriorityQueue<PatrolPoint>();
        HashSet<PatrolPoint> openSet = new HashSet<PatrolPoint>();
        HashSet<PatrolPoint> closedPatrolPoints = new HashSet<PatrolPoint>();

        PatrolPoint startPoint = getNearestPatrolPoint(enemyManager.transform.position).GetComponent<PatrolPoint>();
        PatrolPoint targetPoint = targetPatrolPoint;

        startPoint.Setup(0, startPoint.transform.position, targetPoint.transform.position, null);
        startPoint.UpdateText();

        //move in While loop
        if (drawGizmos)
        {
            startPoint.ChangeGizmoColor(goodPatrolPointColor);
            targetPoint.ChangeGizmoColor(finalPatrolPointColor);
        }

        openPatrolPoints.Enqueue(startPoint, startPoint.GetF());

        while (calculatingPath)
        {
            if (openPatrolPoints.Count == 0)
            {
                print(openPatrolPoints.Count);
                Debug.LogWarning("Open list is empty");
                break;
            }

            PatrolPoint currentPatrolPoint = openPatrolPoints.Dequeue();
            openSet.Remove(currentPatrolPoint);

            if (closedPatrolPoints.Contains(currentPatrolPoint))
                continue;

            closedPatrolPoints.Add(currentPatrolPoint);

            if (currentPatrolPoint == targetPoint)
            {
                finalPatrolPath = ReconstructPath(currentPatrolPoint);
                calculatingPath = false;
                break;
            }

            if (currentPatrolPoint.neighbors != null)
            {
                foreach (PatrolPoint neighbor in currentPatrolPoint.neighbors)
                {
                    if (neighbor == null) continue;
                    if (closedPatrolPoints.Contains(neighbor))
                    {
                        continue;
                    }

                    float tentativeG = currentPatrolPoint.GetG() +
                        (currentPatrolPoint.pos - neighbor.pos).sqrMagnitude;

                    bool isNewNode = !openSet.Contains(neighbor);

                    if (isNewNode || tentativeG < neighbor.GetG())
                    {
                        neighbor.Setup(tentativeG, neighbor.pos, targetPatrolPoint.pos, currentPatrolPoint);
                        openPatrolPoints.Enqueue(neighbor, neighbor.GetF());

                        if (isNewNode)
                            openSet.Add(neighbor);

                        setupVisuals(neighbor);
                    }
                }
            }
        }
    }

    private void setupVisuals(PatrolPoint neighbor)
    {
        if (drawGizmos)
        {
            if (neighbor != targetPatrolPoint)
                neighbor.ChangeGizmoColor(badPatrolPointColor);

            neighbor.UpdateText();
        }

        touchedPatrolPoints.Add(neighbor);
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
                if (path[i] != targetPatrolPoint && drawGizmos)
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

        for (int i = 0; i < n; i++)
            positions[i] = patrolPoints[i].pos;

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
                    float sqrD = (positions[i] - positions[j]).sqrMagnitude;
                    if (sqrD <= searchRadius * searchRadius)
                        validCandidates.Add((patrolPoints[j], sqrD));
                }

                if (validCandidates.Count > 0)
                {
                    validCandidates = validCandidates.OrderBy(x => x.dist).ToList();

                    List<(PatrolPoint p, float dist)> visible = new List<(PatrolPoint p, float dist)>();
                    foreach (var c in validCandidates)
                    {
                        if (visible.Count >= maxNeighbors) break;

                        Vector3 from = positions[i] + RAYCAST_OFFSET;
                        Vector3 to = c.p.pos + RAYCAST_OFFSET;
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
        foreach (var p in touchedPatrolPoints)
            p.Reset();
        touchedPatrolPoints.Clear();
        finalPatrolPath?.Clear();
    }

    public List<Vector3> getAllCurrentPatrolPointPositions()
    {
        List<Vector3> patrolPoints = new List<Vector3>();

        for (int j = 0; j < finalPatrolPath.Count; j++)
        {
            patrolPoints.Add(finalPatrolPath[j].transform.position);
        }

        return patrolPoints;
    }

    public GameObject getNearestPatrolPoint(Vector3 pos)
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return null;

        GameObject smallestDistanceObject = patrolPoints[0].gameObject;
        float smallestDistance = (pos - patrolPoints[0].pos).sqrMagnitude;

        for (int i = 1; i < patrolPoints.Count; i++)
        {
            float d = (pos - patrolPoints[i].pos).sqrMagnitude;
            if (d < smallestDistance)
            {
                smallestDistance = d;
                smallestDistanceObject = patrolPoints[i].gameObject;
            }
        }

        return smallestDistanceObject;
    }
}
