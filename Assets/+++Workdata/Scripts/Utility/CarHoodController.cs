using EasyPeasyFirstPersonController;
using UnityEngine;
using DG.Tweening;

public class CarHoodController : MonoBehaviour, IInteractable
{
    [Header("Hood Settings")]
    public Transform hoodTransform;
    public Vector3 rotationAxis = Vector3.right;
    public float maxOpenAngle = 70f;
    public float animationDuration = 1f;
    public Ease easeType = Ease.OutQuad;

    [Header("Status")]
    public bool isOpen = false;

    private Vector3 closedRotation;
    private Vector3 openRotation;
    private Tween currentTween;

    void Start()
    {
        closedRotation = hoodTransform.localEulerAngles;

        Vector3 axisRotation = rotationAxis.normalized * maxOpenAngle;
        openRotation = closedRotation + axisRotation;

        if (isOpen)
            hoodTransform.localEulerAngles = openRotation;
    }

    public void ToggleHood()
    {
        isOpen = !isOpen;

        currentTween?.Kill();

        Vector3 targetRotation = isOpen ? openRotation : closedRotation;

        currentTween = hoodTransform.DOLocalRotate(targetRotation, animationDuration)
            .SetEase(easeType);
    }

    public void Interact()
    {
        ToggleHood();
    }

    public void OnHoverEnter() { }
    public void OnHoverExit() { }

    void OnDestroy()
    {
        currentTween?.Kill();
    }
}