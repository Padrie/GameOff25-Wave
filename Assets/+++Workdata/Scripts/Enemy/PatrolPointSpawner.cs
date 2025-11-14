using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways] // So you can preview in editor without play mode
public class PatrolPointSpawner : MonoBehaviour
{
    [Header("Patrol Point Settings")]
    public GameObject patrolPointPrefab;
    public LayerMask obstacleMask;

    [Header("Spawn Area")]
    public Vector3 spawnRange = new Vector3(50, 0, 50);
    public Vector2Int gridResolution = new Vector2Int(10, 10);

    [Header("Visualization")]
    public bool showGizmos = true;
    public Color gizmoBoxColor = Color.yellow;
    public Color pointColor = Color.red;
    public float gizmoPointSize = 0.5f;

    [ContextMenu("Spawn Patrol Points")]
    public void SpawnPatrolPoints()
    {
        float stepX = spawnRange.x / (gridResolution.x - 1);
        float stepZ = spawnRange.z / (gridResolution.y - 1);

        Vector3 origin = transform.position - new Vector3(spawnRange.x / 2f, 0f, spawnRange.z / 2f);

        for (int x = 0; x < gridResolution.x; x++)
        {
            for (int z = 0; z < gridResolution.y; z++)
            {
                Vector3 pointPos = origin + new Vector3(x * stepX, 0f, z * stepZ);

                if (Physics.Raycast(pointPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
                    pointPos.y = hit.point.y;
                else
                    pointPos.y = transform.position.y;

                if (Physics.CheckSphere(pointPos, 0.5f, obstacleMask))
                    continue;

                Instantiate(patrolPointPrefab, pointPos, Quaternion.identity);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (showGizmos)
        {
            Gizmos.color = gizmoBoxColor;
            Gizmos.DrawWireCube(transform.position, spawnRange);

            float stepX = spawnRange.x / (gridResolution.x - 1);
            float stepZ = spawnRange.z / (gridResolution.y - 1);

            Vector3 origin = transform.position - new Vector3(spawnRange.x / 2f, 0f, spawnRange.z / 2f);

            for (int x = 0; x < gridResolution.x; x++)
            {
                for (int z = 0; z < gridResolution.y; z++)
                {
                    Vector3 pointPos = origin + new Vector3(x * stepX, 0f, z * stepZ);

                    if (Physics.Raycast(pointPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
                        pointPos.y = hit.point.y;
                    else
                        pointPos.y = transform.position.y;

                    bool blocked = Physics.CheckSphere(pointPos, 0.5f, obstacleMask);

                    Gizmos.color = blocked ? Color.gray : pointColor;
                    Gizmos.DrawSphere(pointPos, gizmoPointSize);
                }
            }
        }
    }
}
