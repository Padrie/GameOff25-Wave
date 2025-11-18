using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InteractionController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionRange = 3f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private IInteractable currentInteractable;
    private HashSet<IInteractable> hoveredInteractables = new HashSet<IInteractable>();
    private RaycastHit[] raycastHits = new RaycastHit[10];

    [Header("Render Layer Settings")]
    [SerializeField] private int outlineRenderingLayer = 0;
    private Dictionary<IInteractable, uint> originalRenderingLayers = new Dictionary<IInteractable, uint>();

    [Header("Crosshair Settings")]
    public RawImage crosshairImage;
    public Texture crosshairImageDefault;
    public Texture crosshairImageOnTarget;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        ChangeRawImage(crosshairImageDefault, Color.white);
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
                RemoveOutline(interactable);
                ChangeRawImage(crosshairImageDefault, Color.white);
            }
        }

        // Handle hover enters for newly detected interactables
        foreach (IInteractable interactable in newHoveredInteractables)
        {
            if (!hoveredInteractables.Contains(interactable))
            {
                interactable.OnHoverEnter();
                CreateOutline(interactable);
                ChangeRawImage(crosshairImageOnTarget, Color.red);
            }
        }

        hoveredInteractables = newHoveredInteractables;
        currentInteractable = closest;
    }

    // Add rendering layer to interactable object
    private void CreateOutline(IInteractable interactable)
    {
        if (originalRenderingLayers.ContainsKey(interactable))
            return; // Already has stored rendering layer

        // Get the GameObject from the interactable
        MonoBehaviour interactableMono = interactable as MonoBehaviour;
        if (interactableMono == null)
            return;

        Renderer renderer = interactableMono.GetComponent<Renderer>();
        if (renderer == null)
            return;

        // Store original rendering layer mask
        originalRenderingLayers[interactable] = renderer.renderingLayerMask;

        // Add outline rendering layer to existing mask
        uint layerMask = 1u << outlineRenderingLayer;
        renderer.renderingLayerMask |= layerMask;
    }

    //Remove rendering layer from interactable object
    private void RemoveOutline(IInteractable interactable)
    {
        if (!originalRenderingLayers.TryGetValue(interactable, out uint originalRenderingLayer))
            return;

        //Get the GameObject from the interactable
        MonoBehaviour interactableMono = interactable as MonoBehaviour;
        if (interactableMono == null)
            return;

        Renderer renderer = interactableMono.GetComponent<Renderer>();
        if (renderer == null)
            return;

        // Restore original rendering layer mask
        renderer.renderingLayerMask = originalRenderingLayer;
        originalRenderingLayers.Remove(interactable);
    }

    // Check for interaction  input
    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }

    private void ChangeRawImage(Texture newTexture, Color newColor)
    {
        crosshairImage.texture = newTexture;
        crosshairImage.color = newColor;
    }

    // Clean up and restore originall rendering layers on disable
    private void OnDisable()
    {
        foreach (var kvp in originalRenderingLayers)
        {
            MonoBehaviour interactableMono = kvp.Key as MonoBehaviour;
            if (interactableMono != null)
            {
                Renderer renderer = interactableMono.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.renderingLayerMask = kvp.Value;
                }
            }
        }
        originalRenderingLayers.Clear();
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
