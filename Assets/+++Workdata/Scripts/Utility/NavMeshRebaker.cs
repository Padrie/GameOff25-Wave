using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class NavMeshRebaker : MonoBehaviour
{
    [Header("NavMesh Settings")]
    public NavMeshSurface navMeshSurface;

    [Header("Rebake Settings")]
    [Range(0f, 2f)]
    public float rebakeDelay = 0.5f;

    private List<Door> doorsToMonitor = new List<Door>();
    private Dictionary<Door, bool> doorStates = new Dictionary<Door, bool>();
    private Coroutine rebakeCoroutine;
    private bool rebakePending = false;
    private bool isCurrentlyBaking = false;

    void Start()
    {
        if (navMeshSurface == null)
        {
            navMeshSurface = FindFirstObjectByType<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                enabled = false;
                return;
            }
        }

        FindAllDoors();
        InitializeDoorStates();
    }

    void Update()
    {
        CheckDoorStates();
    }

    private void FindAllDoors()
    {
        Door[] allDoors = FindObjectsByType<Door>(FindObjectsSortMode.None);
        doorsToMonitor.Clear();
        doorsToMonitor.AddRange(allDoors);
    }

    private void InitializeDoorStates()
    {
        doorStates.Clear();
        foreach (Door door in doorsToMonitor)
        {
            if (door != null)
            {
                doorStates[door] = IsDoorOpen(door);
            }
        }
    }

    private void CheckDoorStates()
    {
        bool stateChanged = false;

        foreach (Door door in doorsToMonitor)
        {
            if (door == null) continue;

            bool currentState = IsDoorOpen(door);
            if (doorStates.ContainsKey(door) && doorStates[door] != currentState)
            {
                doorStates[door] = currentState;
                stateChanged = true;
            }
        }

        if (stateChanged && !rebakePending && !isCurrentlyBaking)
        {
            TriggerRebake();
        }
    }

    private bool IsDoorOpen(Door door)
    {
        float angleFromClosed = Quaternion.Angle(door.transform.rotation, door.GetClosedRotation());
        return angleFromClosed > door.rotationThreshold + 0.1f;
    }

    private void TriggerRebake()
    {
        if (rebakeCoroutine != null)
        {
            StopCoroutine(rebakeCoroutine);
        }
        rebakeCoroutine = StartCoroutine(RebakeAfterDelay());
    }

    private IEnumerator RebakeAfterDelay()
    {
        rebakePending = true;
        yield return new WaitForSeconds(rebakeDelay);

        yield return StartCoroutine(RebakeNavMeshAsync());

        rebakePending = false;
    }

    private IEnumerator RebakeNavMeshAsync()
    {
        if (navMeshSurface == null || isCurrentlyBaking) yield break;

        isCurrentlyBaking = true;

        AsyncOperation asyncOp = navMeshSurface.UpdateNavMesh(navMeshSurface.navMeshData);

        while (!asyncOp.isDone)
        {
            yield return null;
        }

        isCurrentlyBaking = false;
    }

    private void RebakeNavMesh()
    {
        if (navMeshSurface == null) return;
        navMeshSurface.BuildNavMesh();
    }

    public void TriggerImmediateRebake()
    {
        if (rebakeCoroutine != null)
        {
            StopCoroutine(rebakeCoroutine);
            rebakePending = false;
        }
        StartCoroutine(RebakeNavMeshAsync());
    }

    public void AddDoor(Door door)
    {
        if (door != null && !doorsToMonitor.Contains(door))
        {
            doorsToMonitor.Add(door);
            doorStates[door] = IsDoorOpen(door);
        }
    }

    public void RemoveDoor(Door door)
    {
        if (doorsToMonitor.Contains(door))
        {
            doorsToMonitor.Remove(door);
            doorStates.Remove(door);
        }
    }
}