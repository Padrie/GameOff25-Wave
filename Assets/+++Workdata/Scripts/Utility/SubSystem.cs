using EasyPeasyFirstPersonController;
using System;
using UnityEngine;
using UnityEngine.Events;

public class SubSystem : MonoBehaviour
{
    public RepaitItemCategory repairItem;
    public Transform repairItemSlot;

    bool isInTrigger = false;

    FirstPersonController player;
    [Space(10)]
    public UnityEvent onRepaired;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out FirstPersonController p))
        {
            isInTrigger = true;
            player = p;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out FirstPersonController p))
        {
            isInTrigger = false;
            player = null;
        }
    }

    private void Update()
    {
        if (isInTrigger && player.itemSlot != null && Input.GetKeyDown(KeyCode.E))
        {
            if (player.itemSlot.repairItem == repairItem)
            {
                CorrectRepairItem();
            }
            else
            {
                print("Falsches Repair item du opfen");
            }
        }
    }

    public void CorrectRepairItem()
    {
        player.itemSlot.Reparent(repairItemSlot);
        GetComponent<Collider>().enabled = false;
        onRepaired?.Invoke();
        print("Inserted correct repair item");
    }
}
