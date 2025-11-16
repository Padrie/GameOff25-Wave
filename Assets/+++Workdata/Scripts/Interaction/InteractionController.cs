using UnityEngine;
using System.Collections.Generic;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentInteractable;
    private HashSet<IInteractable> hoveredInteractables = new HashSet<IInteractable>();
    private RaycastHit[] raycastHits = new RaycastHit[10]; // Increase if needed

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        CheckForInteractable();
        HandleInteractionInput();
    }

    // Raycast from camera to detect nultiple interactable objects in range
    private void CheckForInteractable()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, interactionRange, interactableLayer);

        HashSet<IInteractable> newHoveredInteractables = new HashSet<IInteractable>();
        IInteractable closest = null;
        float closestDistance = float.MaxValue;

        // Check all hits and find the closest interactable
        for (int i = 0; i < hitCount; i++)
        {
            IInteractable interactable = raycastHits[i].collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                newHoveredInteractables.Add(interactable);

                // Track closest interactable
                if (raycastHits[i].distance < closestDistance)
                {
                    closestDistance = raycastHits[i].distance;
                    closest = interactable;
                }

                // Update hit info for closest
                if (interactable is IInteractableWithHit && raycastHits[i].distance == closestDistance)
                {
                    ((IInteractableWithHit)interactable).UpdateHitInfo(raycastHits[i]);
                }
            }
        }

        // Handle hover exits for interactables no longer in raycast
        foreach (IInteractable interactable in hoveredInteractables)
        {
            if (!newHoveredInteractables.Contains(interactable))
            {
                interactable.OnHoverExit();
            }
        }

        // Handle hover enters for newly detected interactables
        foreach (IInteractable interactable in newHoveredInteractables)
        {
            if (!hoveredInteractables.Contains(interactable))
            {
                interactable.OnHoverEnter();
            }
        }

        hoveredInteractables = newHoveredInteractables;
        currentInteractable = closest;
    }

    // Check for interaction input
    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    private void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(ray.origin, ray.direction * interactionRange);
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(ray.origin + ray.direction * interactionRange, 0.1f);
        }
    }
}