using EasyPeasyFirstPersonController;
using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(LightSystem))]
public class SubSystem : MonoBehaviour, IInteractable
{
    public RepairItemCategory repairItem;
    public Transform repairItemSlot;

    FirstPersonController _firstPersonController;

    [Space(10)]
    public UnityEvent CallWhenRepaired;
    public static event Action OnRepaired;

    private void Awake()
    {
        _firstPersonController = FindFirstObjectByType<FirstPersonController>();
    }

    public void CorrectRepairItem()
    {
        _firstPersonController.itemSlot.Reparent(repairItemSlot);
        GetComponent<Collider>().enabled = false;
        CallWhenRepaired?.Invoke();
        OnRepaired?.Invoke();

        print("Inserted correct repair item");
    }

    public void Interact()
    {
        // Skip if player holds no item
        if (_firstPersonController.itemSlot == null) return;

        if (_firstPersonController.itemSlot.repairItem == repairItem)
        {
            CorrectRepairItem();
        }
        else
        {
            print("Falsches Repair item du opfer");
        }
    }

    public void OnHoverEnter() { }
    public void OnHoverExit() { }
}
