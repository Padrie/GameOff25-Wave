using EasyPeasyFirstPersonController;
using UnityEngine;

public class SubSystem : MonoBehaviour
{
    public RepaitItemCategory repairItem;
    public Transform repairItemSlot;

    bool isInTrigger = false;

    FirstPersonController player;

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
        }
    }

    public void CorrectRepairItem()
    {
        player.itemSlot.Reparent(repairItemSlot);
        GetComponent<Collider>().enabled = false;
    }
}
