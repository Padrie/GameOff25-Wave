using UnityEngine;
using UnityEngine.Events;

public class InteractableObject : MonoBehaviour, IInteractable
{
    [SerializeField] private string objectName = "Object";
    [SerializeField] private bool canInteractMultipleTimes = true;

    [Space]
    [SerializeField] private UnityEvent onInteracted;

    private bool hasBeenInteracted = false;

    //Called when player interacts with this object
    public void Interact()
    {
        if (!canInteractMultipleTimes && hasBeenInteracted)
        {
            return;
        }
        hasBeenInteracted = true;
        Debug.Log($"Interacted with {objectName}!");

        onInteracted?.Invoke();
        OnInteracted();
    }

    //Called when player starts looking at this object
    public void OnHoverEnter()
    {
        Debug.Log($"Looking at {objectName}");
    }

    //Called when player stops looking at this object
    public void OnHoverExit()
    {
        Debug.Log($"Stopped looking at {objectName}");
    }

    //Override this in derived classes for custom interaction behavior
    protected virtual void OnInteracted()
    {
    }
}