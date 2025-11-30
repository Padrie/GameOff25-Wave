using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class DoorNavMeshObstacle : MonoBehaviour
{
    public Door door;
    public Vector3 obstacleSize = new Vector3(1f, 2f, 0.3f);
    public float openAngleThreshold = 30f;

    [Header("Debug")]
    public float currentAngle = 0f;
    public bool isDoorOpen = false;

    private NavMeshObstacle obstacle;
    private Quaternion closedRotation;
    private bool initialized = false;

    void Start()
    {
        obstacle = GetComponent<NavMeshObstacle>();

        if (door == null)
        {
            door = GetComponent<Door>();
            if (door == null)
            {
                door = GetComponentInParent<Door>();
            }
        }

        //Configure obstacle
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = obstacleSize;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        obstacle.carvingMoveThreshold = 0.1f;
        obstacle.carvingTimeToStationary = 0.1f;

        //Get closed rotation from Door script
        if (door != null)
        {
            closedRotation = door.GetClosedRotation();
            Debug.Log($"[DoorNavMesh] Closed rotation set to: {closedRotation.eulerAngles}");
        }
        else
        {
            closedRotation = transform.rotation;
            Debug.LogWarning($"[DoorNavMesh] No Door found, using current rotation as closed");
        }

        initialized = true;
        UpdateState();
    }

    void Update()
    {
        if (initialized)
        {
            UpdateState();
        }
    }

    void UpdateState()
    {
        if (door != null)
        {
            currentAngle = Quaternion.Angle(door.transform.rotation, closedRotation);
            isDoorOpen = currentAngle > openAngleThreshold;
        }
        else
        {
            currentAngle = 0f;
            isDoorOpen = false;
        }

        //Block when CLOSED, allow passage when OPEN
        obstacle.enabled = !isDoorOpen;
        obstacle.carving = !isDoorOpen;
    }

}