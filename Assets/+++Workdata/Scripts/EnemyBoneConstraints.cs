using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class EnemyBoneConstraints : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private EnemyManager manager;
    [SerializeField] private List<LookAtConstraint> lookAtConstraints;

    [Header("Settings")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float minWeight = .5f;
    [SerializeField] private float maxWeight = 1f;

    [Header("Active States")]
    [SerializeField]
    private List<string> activeStates;
    private float target;
    private float randomWeight;

    private void Awake()
    {
        if (manager == null)
            manager = GetComponent<EnemyManager>();
    }

    private void Update()
    {
        bool shouldLook = ShouldLook();

        if (shouldLook && target == 0f)
        {
            randomWeight = Random.Range(minWeight, maxWeight);
        }

        target = shouldLook ? randomWeight : 0f;

        foreach (var constraint in lookAtConstraints)
        {
            if (constraint != null)
                constraint.weight = Mathf.Lerp(constraint.weight, target, Time.deltaTime * speed);
        }
    }

    private bool ShouldLook()
    {
        if (string.IsNullOrEmpty(manager.currentState))
            return false;

        foreach (var state in activeStates)
        {
            if (manager.currentState.Equals(state))
                return true;
        }
        return false;
    }
}