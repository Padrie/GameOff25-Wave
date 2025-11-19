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
    private Dictionary<IInteractable, List<RendererData>> originalRenderingLayers = new Dictionary<IInteractable, List<RendererData>>();

    [Header("Crosshair Settings")]
    public RawImage crosshairImage;
    public Texture crosshairImageDefault;
    public Texture crosshairImageOnTarget;

    // Helper class to store renderer and its original rendering layer
    private class RendererData
    {
        public Renderer renderer;
        public uint originalRenderingLayerMask;

        public RendererData(Renderer renderer, uint originalRenderingLayerMask)
        {
            this.renderer = renderer;
            this.originalRenderingLayerMask = originalRenderingLayerMask;
        }
    }

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

    // Raycast from camera to detect multiple interactable objects in range
    private void CheckForInteractable()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int hitCount = Physics.RaycastNonAlloc(ray, raycastHits, interactionRange, interactableLayer);

        HashSet<IInteractable> newHoveredInteractables = new HashSet<IInteractable>();
        IInteractable closest = null;
        float closestDistance = float.MaxValue;
        int closestHitIndex = -1; // Track which hit was closest

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
                    closestHitIndex = i; // Store the index
                }
            }
        }

        // Update hit info for the closest interactable AFTER the loop
        if (closestHitIndex >= 0 && closest is IInteractableWithHit)
        {
            ((IInteractableWithHit)closest).UpdateHitInfo(raycastHits[closestHitIndex]);
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


    // Add rendering layer to interactable object and all its children with renderers
    private void CreateOutline(IInteractable interactable)
    {
        if (originalRenderingLayers.ContainsKey(interactable))
            return; // Already has stored rendering layer

        // Get the GameObject from the interactable
        MonoBehaviour interactableMono = interactable as MonoBehaviour;
        if (interactableMono == null)
            return;

        // Get all renderers (parent and children)
        Renderer[] renderers = interactableMono.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        List<RendererData> rendererDataList = new List<RendererData>();

        // Store original rendering layer mask for each renderer and apply outline
        foreach (Renderer renderer in renderers)
        {
            // Store original rendering layer mask
            rendererDataList.Add(new RendererData(renderer, renderer.renderingLayerMask));

            // Add outline rendering layer to existing mask
            uint layerMask = 1u << outlineRenderingLayer;
            renderer.renderingLayerMask |= layerMask;
        }

        originalRenderingLayers[interactable] = rendererDataList;
    }

    //Remove rendering layer from interactable object and all its children with renderers
    private void RemoveOutline(IInteractable interactable)
    {
        if (!originalRenderingLayers.TryGetValue(interactable, out List<RendererData> rendererDataList))
            return;

        // Restore original rendering layer mask for each renderer
        foreach (RendererData rendererData in rendererDataList)
        {
            if (rendererData.renderer != null)
            {
                rendererData.renderer.renderingLayerMask = rendererData.originalRenderingLayerMask;
            }
        }

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
            foreach (RendererData rendererData in kvp.Value)
            {
                if (rendererData.renderer != null)
                {
                    rendererData.renderer.renderingLayerMask = rendererData.originalRenderingLayerMask;
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