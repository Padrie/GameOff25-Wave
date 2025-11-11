using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    public bool drawGizmos = true;

    Color patrolPointColor = Color.yellow;

    [HideInInspector, Tooltip("Cost of distance from starting node")] float gScore = 0;
    [HideInInspector, Tooltip("Cost of distance from end node")] float hScore = 0;
    [HideInInspector, Tooltip("Total estimated cost")] float fScore = 0;

    [SerializeField] TextMeshPro gText;
    [SerializeField] TextMeshPro hText;
    [SerializeField] TextMeshPro fText;

    [HideInInspector] public PatrolPoint cameFrom;
    public List<PatrolPoint> neighbors;

    public void ChangeGizmoColor(Color color)
    {
        patrolPointColor = color;
    }

    public void UpdateText()
    {
        gText.text = "G=" + gScore;
        hText.text = "H=" + hScore;
        fText.text = "F=" + fScore;
    }

    public void Setup(float g, Vector3 neighborPos, Vector3 goalPos, PatrolPoint cameFrom)
    {
        SetG(g);
        SetH(neighborPos, goalPos);
        SetF();
        this.cameFrom = cameFrom;
    }

    public void Reset()
    {
        cameFrom = null;
        gScore = 0;
        hScore = 0;
        fScore = 0;
        ChangeGizmoColor(Color.yellow);
        UpdateText();
    }

    public void SetG(float score)
    {
        gScore = score;
    }

    public float GetG()
    {
        return gScore;
    }

    private void SetH(Vector3 start, Vector3 goal)
    {
        hScore = Vector3.Distance(start, goal);
    }

    public float GetH()
    {
        return hScore;
    }

    private void SetF()
    {
        fScore = GetG() + hScore;
    }

    public float GetF()
    {
        return fScore;
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
