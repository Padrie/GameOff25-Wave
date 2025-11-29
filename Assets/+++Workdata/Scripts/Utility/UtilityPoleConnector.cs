using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class UtilityPoleConnector : MonoBehaviour
{
    [Header("Connector References")]
    public Transform connectorA;
    public Transform connectorB;

    [Header("Auto-Find Settings")]
    public string connectorAName = "Wire Connector A";
    public string connectorBName = "Wire Connector B";

    private void OnEnable()
    {
        FindConnectors();
    }

    private void Reset()
    {
        FindConnectors();
    }

    [ContextMenu("Find Connectors")]
    public void FindConnectors()
    {
        if (connectorA == null)
            connectorA = FindChildByName(connectorAName);
        if (connectorB == null)
            connectorB = FindChildByName(connectorBName);
    }

    private Transform FindChildByName(string name)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child.name == name)
                return child;
        }
        return null;
    }

    public Transform GetConnector(int index)
    {
        switch (index)
        {
            case 0: return connectorA;
            case 1: return connectorB;
            default: return null;
        }
    }

    public int GetConnectorCount()
    {
        int count = 0;
        if (connectorA != null) count++;
        if (connectorB != null) count++;
        return count;
    }

    public Transform[] GetAllConnectors()
    {
        return new Transform[] { connectorA, connectorB };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EditorApplication.delayCall += () =>
        {
            if (this != null)
            {
                FindConnectors();
            }
        };
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        if (connectorA != null)
            Gizmos.DrawWireSphere(connectorA.position, 0.1f);

        Gizmos.color = Color.green;
        if (connectorB != null)
            Gizmos.DrawWireSphere(connectorB.position, 0.1f);
    }
#endif
}