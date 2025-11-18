using EasyPeasyFirstPersonController;

using UnityEngine;

public class RepairItem : MonoBehaviour
{
    public RepaitItemCategory repairItem;
    [SerializeField] SoundStrength dropSoundStrength;
    [SerializeField] LayerMask dropItemLayerMask;

    bool isPlayerHolding = false;
    bool isPlayerInTrigger = false;
    FirstPersonController player;

    Collider[] colliders;
    Material[] materials;

    Renderer renderer;
    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();

        renderer = GetComponentInChildren<Renderer>();
        materials = renderer.materials;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlayerHolding) return;

        if (other.TryGetComponent(out FirstPersonController p))
        {
            player = p;
            isPlayerInTrigger = true;
            print($"{player} has entered the Collider");

            foreach (Material m in materials)
            {
                if (m.shader.name == "Shader Graphs/OutlineShader")
                {
                    m.SetFloat("_outlineEnabled", 1f);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isPlayerHolding) return;

        if (other.TryGetComponent(out FirstPersonController p) && p == player)
        {
            isPlayerInTrigger = false;
            player = null;
            print($"Player has exited the Collider");

            foreach (Material m in materials)
            {
                if (m.shader.name == "Shader Graphs/OutlineShader")
                {
                    m.SetFloat("_outlineEnabled", 0f);
                }
            }
        }
    }

    private void Update()
    {
        if (!isPlayerHolding && isPlayerInTrigger && player != null && Input.GetKeyDown(KeyCode.E))
        {
            PickupItem();
        }

        if (isPlayerHolding && Input.GetKeyDown(KeyCode.Q))
        {
            DropItem();
        }
    }

    public void PickupItem()
    {
        if (player.itemSlot != null)
            player.itemSlot.DropItem();

        isPlayerHolding = true;

        player.itemSlot = this;

        transform.SetParent(player.itemHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        foreach (Material m in materials)
        {
            if (m.shader.name == "Shader Graphs/OutlineShader")
            {
                m.SetFloat("_outlineEnabled", 0f);
            }
        }

        DisableGravity();
        print("Picked up item");
    }

    public void DropItem()
    {
        isPlayerHolding = false;

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
        isPlayerHolding = false;

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
