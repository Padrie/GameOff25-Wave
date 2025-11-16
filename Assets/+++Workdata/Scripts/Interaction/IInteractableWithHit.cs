using UnityEngine;

public interface IInteractableWithHit : IInteractable
{
    void UpdateHitInfo(RaycastHit hit);
}
