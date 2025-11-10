using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    public bool drawGizmos = true;

    Color patrolPointColor = Color.yellow;

    [HideInInspector, Tooltip("Cost of distance from starting node")] float gScore = 0;
    [HideInInspector, Tooltip("Cost of distance from end node")] float hScore = 0;
    [HideInInspector, Tooltip("Total estimated cost")] float fScore = 0;

    [HideInInspector] public PatrolPoint cameFrom;

    public void ChangeGizmoColor(Color color)
    {
        patrolPointColor = color;
    }

    public float G()
    {
        return gScore;
    }

    public float H(Vector3 start, Vector3 goal)
    {
        return Vector3.Distance(start, goal);
    }

    public float F(Vector3 start, Vector3 goal)
    {
        return G() + H(start, goal);
    }

    private void OnDrawGizmos()
    {
        if (drawGizmos)
        {
            Gizmos.color = patrolPointColor;
            Gizmos.DrawSphere(transform.position, 0.5f);
        }
    }
}
