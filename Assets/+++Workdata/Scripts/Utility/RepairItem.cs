using EasyPeasyFirstPersonController;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RepairItem : MonoBehaviour, IInteractable
{
    [Header("Item Settings")]
    public RepairItemCategory repairItem;

    [Header("Model Parent")]
    [SerializeField] GameObject itemModelParent; // Assign in Inspector - empty child object

    [Header("Sound Settings")]
    [SerializeField] SoundStrength dropSoundStrength;
    [SerializeField] LayerMask dropItemLayerMask;

    bool holdsItem = false;
    FirstPersonController _firstPersonController;
    Collider[] colliders;
    Material[] materials;
    Renderer renderer;
    Rigidbody rb;
    [SerializeField] TextMeshProUGUI itemNameDisplayText;

    private RepairItemCategory lastRepairItem;
    private GameObject spawnedModel;
    [SerializeField] private RepairItemConfig itemConfig;

    private void Start()
    {
        _firstPersonController = FindFirstObjectByType<FirstPersonController>();
        rb = GetComponent<Rigidbody>();
        colliders = GetComponents<Collider>();
        renderer = GetComponentInChildren<Renderer>();
        //materials = renderer.materials;
        itemNameDisplayText.enabled = false;
        itemNameDisplayText.text = repairItem.ToString();

        lastRepairItem = repairItem;

        //Spawn initial model
        SpawnModel();
        UpdateTextPosition();
    }

    private void OnValidate()
    {
        //Update the text display when values change in editor
        if (itemNameDisplayText != null)
        {
            itemNameDisplayText.text = repairItem.ToString();
            UpdateTextPosition();
        }

        if (repairItem != lastRepairItem)
        {
            lastRepairItem = repairItem;

#if UNITY_EDITOR

            return;
            //In edit mode, update the model after new frame to avoid prefab issues
            EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    SpawnModel();
                    UpdateTextPosition();
                }
            };
#endif
        }
    }


    private void UpdateTextPosition()
    {
        if (itemNameDisplayText != null && itemConfig != null)
        {
            var settings = itemConfig.GetSettings(repairItem);
            if (settings != null)
            {
                Vector3 pos = itemNameDisplayText.rectTransform.position;
                pos.y = transform.position.y + settings.textOffsetY;
                itemNameDisplayText.rectTransform.position = pos;
            }
        }
    }

    private GameObject GetCurrentPrefab()
    {
        if (itemConfig != null)
        {
            var settings = itemConfig.GetSettings(repairItem);
            if (settings != null && settings.prefab != null)
                return settings.prefab;
        }
        return null;
    }

    private void SpawnModel()
    {
        if (itemModelParent == null) return;

        //Destroy all children of itemModelParent
        for (int i = itemModelParent.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = itemModelParent.transform.GetChild(i);
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject, true);
            else
#endif
                Destroy(child.gameObject);
        }

        spawnedModel = null;

        //Spawn new model based on the current RepairItem
        GameObject prefab = GetCurrentPrefab();
        if (prefab != null)
        {
            spawnedModel = Instantiate(prefab, itemModelParent.transform);
            spawnedModel.transform.localPosition = Vector3.zero;
            spawnedModel.transform.localRotation = Quaternion.identity;
        }
    }

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
        if (holdsItem && Input.GetKeyDown(KeyCode.Q))
        {
            DropItem();
        }
    }

    public void PickupItem()
    {
        var player = _firstPersonController;

        if (player.itemSlot != null)
        {
            if (player.isHoldingCheckList)
            {
                player.DeactivateCheckList();
            }
            else
                player.itemSlot.GetComponent<RepairItem>().DropItem();
        }

        holdsItem = true;
        player.itemSlot = gameObject;
        transform.SetParent(player.itemHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
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
    }

    public void Reparent(Transform newParent)
    {
        var player = _firstPersonController;
        holdsItem = false;
        player.itemSlot = null;
        player = null;

        Destroy(gameObject);
        return;
        transform.SetParent(newParent);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
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