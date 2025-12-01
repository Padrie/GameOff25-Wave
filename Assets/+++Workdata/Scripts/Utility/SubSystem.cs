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
    public static event Action<RepairItemCategory> OnRepaired;

    private AudioPlayer audioPlayer;

    private void Awake()
    {
        _firstPersonController = FindFirstObjectByType<FirstPersonController>();
        audioPlayer = FindAnyObjectByType<AudioPlayer>();
    }

    public void CorrectRepairItem()
    {
        _firstPersonController.itemSlot.GetComponent<RepairItem>().Reparent(repairItemSlot);
        CallWhenRepaired?.Invoke();
        OnRepaired?.Invoke(repairItem);

        print("Inserted correct repair item");
    }

    public void Interact()
    {
        // Skip if player holds no item
        if (_firstPersonController.itemSlot == null) return;

        if (_firstPersonController.itemSlot.GetComponent<RepairItem>().repairItem == repairItem)
        {
            CorrectRepairItem();
            audioPlayer.PlayCorrectRepairItemSFX(transform.position);
        }
        else
        {
            print("Falsches Repair item du opfer");
            audioPlayer.PlayFalseRepairItemSFX(transform.position);
        }
    }

    public void OnHoverEnter() { }
    public void OnHoverExit() { }
}
