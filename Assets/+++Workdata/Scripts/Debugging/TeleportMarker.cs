using UnityEngine;

/// <summary>
/// Visual marker for teleport points. Place these in your scene to mark important locations.
/// The DebugTeleporter will automatically detect these on start.
/// </summary>
public class TeleportMarker : MonoBehaviour
{
    [SerializeField] private string pointName = "Teleport Point";
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private float gizmoSize = 1f;
    [SerializeField] private bool showLabel = true;

    void OnDrawGizmos()
    {
        // Draw sphere
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, gizmoSize);
        
        // Draw direction indicator
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * gizmoSize * 1.5f;
        Gizmos.DrawRay(transform.position, forward);
        
        // Draw upward line
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * gizmoSize * 2f);

#if UNITY_EDITOR
        if (showLabel)
        {
            UnityEditor.Handles.Label(transform.position + Vector3.up * gizmoSize * 2.5f, pointName);
        }
#endif
    }

    void OnDrawGizmosSelected()
    {
        // Highlight when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(transform.position, gizmoSize * 0.2f);
    }

    public string GetPointName() => pointName;
}
