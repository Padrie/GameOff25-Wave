using EasyPeasyFirstPersonController;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using TMPro;

public class RepairItem : MonoBehaviour, IInteractable
{
    public RepaitItemCategory repairItem;
    [SerializeField] SoundStrength dropSoundStrength;
    [SerializeField] LayerMask dropItemLayerMask;

    bool holdsItem = false;
    //bool isPlayerInTrigger = false;
    FirstPersonController _firstPersonController;

    Collider[] colliders;
    Material[] materials;

    Renderer renderer;
    Rigidbody rb;


    [SerializeField] TextMeshProUGUI itemNameDisplayText;

    private void Awake()
    {
        _firstPersonController = FindFirstObjectByType<FirstPersonController>();

        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();

        renderer = GetComponentInChildren<Renderer>();
        materials = renderer.materials;

        itemNameDisplayText.enabled = false;
        itemNameDisplayText.text = repairItem.ToString();
    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    if (isPlayerHolding) return;

    //    if (other.TryGetComponent(out FirstPersonController p))
    //    {
    //        player = p;
    //        isPlayerInTrigger = true;
    //        print($"{player} has entered the Collider");

    //        foreach (Material m in materials)
    //        {
    //            if (m.shader.name == "Shader Graphs/OutlineShader")
    //            {
    //                m.SetFloat("_outlineEnabled", 1f);
    //            }
    //        }
    //    }
    //}

    //private void OnTriggerExit(Collider other)
    //{
    //    if (isPlayerHolding) return;

    //    if (other.TryGetComponent(out FirstPersonController p) && p == player)
    //    {
    //        isPlayerInTrigger = false;
    //        player = null;
    //        print($"Player has exited the Collider");

    //        foreach (Material m in materials)
    //        {
    //            if (m.shader.name == "Shader Graphs/OutlineShader")
    //            {
    //                m.SetFloat("_outlineEnabled", 0f);
    //            }
    //        }
    //    }
    //}


    public void Interact()
    {
        if (holdsItem) return;
        PickupItem();

        ToggleItemTextDisplay(false);
    }

    public void OnHoverEnter() 
    {
        ToggleItemTextDisplay(true);
    }
    public void OnHoverExit() 
    {
        ToggleItemTextDisplay(false);
    }


    private void ToggleItemTextDisplay(bool enable)
    {
        itemNameDisplayText.enabled = enable;
    }

    private void Update()
    {
        //if (!isPlayerHolding && isPlayerInTrigger && player != null && Input.GetKeyDown(KeyCode.E))
        //{
        //    PickupItem();
        //}

        if (holdsItem && Input.GetKeyDown(KeyCode.Q))
        {
            DropItem();
        }
    }

    public void PickupItem()
    {
        var player = _firstPersonController;

        if (player.itemSlot != null)
            player.itemSlot.DropItem();

        holdsItem = true;

        player.itemSlot = this;

        transform.SetParent(player.itemHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        //foreach (Material m in materials)
        //{
        //    if (m.shader.name == "Shader Graphs/OutlineShader")
        //    {
        //        m.SetFloat("_outlineEnabled", 0f);
        //    }
        //}

        DisableGravity();
        print("Picked up item");
    }

    public void DropItem()
    {
        var player = _firstPersonController;

        holdsItem = false;

        player.itemSlot = null;

        transform.SetParent(null);
        transform.position = player.transform.position;

        SoundManager.EmitSound(transform.position, dropSoundStrength);

        player = null;
        EnableGravity();

        //if (Physics.Raycast(pos, Vector3.down, out RaycastHit hit, 10, dropItemLayerMask))
        //{
        //    transform.position = hit.point;
        //    isPlayerHolding = false;
        //}
    }

    public void Reparent(Transform newParent)
    {
        var player = _firstPersonController;

        holdsItem = false;

        player.itemSlot = null;

        transform.SetParent(newParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        player = null;
    }

    public void EnableGravity()
    {
        foreach (Collider c in colliders)
        {
            c.enabled = true;
        }

        rb.isKinematic = false;
    }

    public void DisableGravity()
    {
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        rb.isKinematic = true;
    }
}
