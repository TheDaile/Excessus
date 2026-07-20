using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private const int DefaultQuickUseSlotCount = 3;

    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private GameObject descriptionPanel;
    [SerializeField] private TextMeshProUGUI descriptionTitleText;
    [SerializeField] private TextMeshProUGUI descriptionBodyText;

    [FormerlySerializedAs("slotUis")]
    [SerializeField] private List<InventorySlotUI> inventorySlotUis = new List<InventorySlotUI>();
    [SerializeField] private List<InventorySlotUI> weaponSlotUis = new List<InventorySlotUI>();
    [SerializeField] private List<InventorySlotUI> quickUseSlotUis = new List<InventorySlotUI>();

    [Header("Player")]
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private WeaponEquipment weaponEquipment;

    [Header("Input")]
    [SerializeField] private bool toggleWithLegacyInput;
    [SerializeField] private bool createEventSystemIfMissing = true;

    private readonly List<InventorySlot> inventorySlots = new List<InventorySlot>();
    private readonly List<InventorySlot> weaponSlots = new List<InventorySlot>();
    private readonly List<InventorySlot> quickUseSlots = new List<InventorySlot>();
    private bool isOpen;

    public bool IsOpen => isOpen;

    private void Awake()
    {
        ResolveReferences();
        EnsureEventSystem();
        BuildSlots();
        SetOpen(false);
        ClearDescription();
    }

    private void Update()
    {
        if (toggleWithLegacyInput && Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        SetOpen(!isOpen);
    }

    public void SetOpen(bool open)
    {
        isOpen = open;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(isOpen);
        }

        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Time.timeScale = 1f;
    }

    public int AddItem(InventoryItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            return 0;
        }

        EnsureSlotsBuilt();

        // If this is a weapon, try to place it into weapon slots first.
        if (item.ItemType == InventoryItemType.Weapon)
        {
            int remaining = amount;
            while (remaining > 0)
            {
                int emptyWeaponIndex = FindEmptySlot(weaponSlots);
                if (emptyWeaponIndex < 0)
                {
                    break;
                }

                weaponSlots[emptyWeaponIndex].Set(item, 1);
                remaining--;
            }

            if (amount - remaining > 0)
            {
                SyncSpecialSlots();
                RefreshAllSlots();
                // Return number actually added into weapon slots (we treat each weapon as amount 1)
                return amount - remaining;
            }
            // If no weapon slot was available, fall through to normal inventory addition.
        }

        if (item.IsStackable)
        {
            int stackSlotIndex = FindStackSlot(inventorySlots, item);
            if (stackSlotIndex >= 0)
            {
                inventorySlots[stackSlotIndex].Add(amount);
                RefreshAllSlots();
                return amount;
            }

            int emptySlotIndex = FindEmptySlot(inventorySlots);
            if (emptySlotIndex < 0)
            {
                return 0;
            }

            inventorySlots[emptySlotIndex].Set(item, amount);
            RefreshAllSlots();
            return amount;
        }

        int remainingAmount = amount;
        while (remainingAmount > 0)
        {
            int emptySlotIndex = FindEmptySlot(inventorySlots);
            if (emptySlotIndex < 0)
            {
                break;
            }

            inventorySlots[emptySlotIndex].Set(item, 1);
            remainingAmount--;
        }

        RefreshAllSlots();
        return amount - remainingAmount;
    }

    public bool UseSlot(int slotIndex)
    {
        return UseSlot(InventorySlotKind.Inventory, slotIndex);
    }

    public bool UseSlot(InventorySlotKind slotKind, int slotIndex)
    {
        InventorySlot slot = GetSlot(slotKind, slotIndex);
        if (slot == null || slot.IsEmpty)
        {
            return false;
        }

        InventoryItemData item = slot.Item;

        switch (item.ItemType)
        {
            case InventoryItemType.Consumable:
                return UseConsumable(slotKind, slotIndex, item);
            case InventoryItemType.Plot:
                Debug.Log("It's Plot item");
                return false;
            case InventoryItemType.Weapon:
                return EquipWeaponFromSlot(slotKind, slotIndex);
            case InventoryItemType.Key:
                Debug.Log("Use keys by pressing E on a locked door.");
                return false;
            default:
                Debug.Log("This item cannot be used yet: " + item.ItemName);
                return false;
        }
    }

    public bool UseQuickSlot(int slotIndex)
    {
        return UseSlot(InventorySlotKind.QuickUse, slotIndex);
    }

    public bool DropSlotItem(InventorySlotKind slotKind, int slotIndex)
    {
        return DropSlotAmount(slotKind, slotIndex, -1);
    }

    public bool DropSlotAmount(InventorySlotKind slotKind, int slotIndex, int amount)
    {
        InventorySlot slot = GetSlot(slotKind, slotIndex);
        if (slot == null || slot.IsEmpty)
        {
            return false;
        }

        Vector3 dropPosition = GetDropPosition();

        int dropAmount;
        if (amount <= 0)
        {
            // default behavior: if amount not provided or <=0, drop one for stackable inventory items, otherwise drop whole stack
            dropAmount = slot.Item != null && slot.Item.IsStackable && slotKind == InventorySlotKind.Inventory ? 1 : slot.Amount;
        }
        else
        {
            dropAmount = Mathf.Clamp(amount, 1, slot.Amount);
        }

        if (!CreateDropPickup(slot.Item, dropAmount, dropPosition))
        {
            return false;
        }

        RemoveFromSlot(slotKind, slotIndex, dropAmount);
        return true;
    }

    public void MoveSlot(int fromIndex, int toIndex)
    {
        MoveSlot(InventorySlotKind.Inventory, fromIndex, InventorySlotKind.Inventory, toIndex);
    }

    public bool MoveSlot(InventorySlotKind fromKind, int fromIndex, InventorySlotKind toKind, int toIndex)
    {
        if (!IsValidSlotIndex(fromKind, fromIndex) || !IsValidSlotIndex(toKind, toIndex))
        {
            return false;
        }

        if (fromKind == toKind && fromIndex == toIndex)
        {
            return false;
        }

        InventorySlot fromSlot = GetSlot(fromKind, fromIndex);
        InventorySlot toSlot = GetSlot(toKind, toIndex);

        if (fromSlot == null || toSlot == null || fromSlot.IsEmpty)
        {
            return false;
        }

        if (!CanPlaceItemInSlotKind(toKind, fromSlot.Item))
        {
            Debug.Log(GetRejectedSlotMessage(toKind));
            return false;
        }

        if (!toSlot.IsEmpty && !CanPlaceItemInSlotKind(fromKind, toSlot.Item))
        {
            Debug.Log(GetRejectedSlotMessage(fromKind));
            return false;
        }

        if (toSlot.CanStack(fromSlot.Item))
        {
            toSlot.Add(fromSlot.Amount);
            fromSlot.Clear();
        }
        else
        {
            SwapSlotContents(fromSlot, toSlot);
        }

        SyncSpecialSlots();
        RefreshAllSlots();
        return true;
    }

    public bool SelectWeaponSlot(int slotIndex)
    {
        if (!IsValidSlotIndex(InventorySlotKind.Weapon, slotIndex) || weaponSlots[slotIndex].IsEmpty)
        {
            return false;
        }

        ResolveWeaponEquipment();

        bool selected = weaponEquipment != null && weaponEquipment.SelectWeapon(slotIndex);
        RefreshAllSlots();
        return selected;
    }

    public void ShowSlotDescription(InventorySlotKind slotKind, int slotIndex)
    {
        InventorySlot slot = GetSlot(slotKind, slotIndex);

        if (slot == null || slot.IsEmpty)
        {
            ClearDescription();
            return;
        }

        ShowItemDescription(slot.Item, slot.Amount);
    }

    public bool HasKey(string keyId)
    {
        return ContainsKey(inventorySlots, keyId) ||
               ContainsKey(weaponSlots, keyId) ||
               ContainsKey(quickUseSlots, keyId);
    }

    public bool ConsumeKey(string keyId)
    {
        bool consumed =
            ConsumeKeyFromSlots(inventorySlots, keyId) ||
            ConsumeKeyFromSlots(weaponSlots, keyId) ||
            ConsumeKeyFromSlots(quickUseSlots, keyId);

        if (consumed)
        {
            RefreshAllSlots();
        }

        return consumed;
    }

    public InventorySlot GetSlot(int slotIndex)
    {
        return GetSlot(InventorySlotKind.Inventory, slotIndex);
    }

    public InventorySlot GetSlot(InventorySlotKind slotKind, int slotIndex)
    {
        List<InventorySlot> slotList = GetSlotList(slotKind);
        return slotIndex >= 0 && slotIndex < slotList.Count ? slotList[slotIndex] : null;
    }

    private void BuildSlots()
    {
        inventorySlotUis.RemoveAll(slotUi => slotUi == null);
        weaponSlotUis.RemoveAll(slotUi => slotUi == null);
        quickUseSlotUis.RemoveAll(slotUi => slotUi == null);

        if (inventorySlotUis.Count == 0 || weaponSlotUis.Count == 0 || quickUseSlotUis.Count == 0)
        {
            FindSlotsInPanel();
        }

        inventorySlots.Clear();
        weaponSlots.Clear();
        quickUseSlots.Clear();

        for (int i = 0; i < inventorySlotUis.Count; i++)
        {
            inventorySlots.Add(new InventorySlot());
            inventorySlotUis[i].Configure(this, InventorySlotKind.Inventory, i);
        }

        int weaponSlotCount = Mathf.Max(weaponSlotUis.Count, GetWeaponSlotCount());
        for (int i = 0; i < weaponSlotCount; i++)
        {
            weaponSlots.Add(new InventorySlot());
        }

        for (int i = 0; i < weaponSlotUis.Count; i++)
        {
            weaponSlotUis[i].Configure(this, InventorySlotKind.Weapon, i);
        }

        int quickUseSlotCount = Mathf.Max(quickUseSlotUis.Count, DefaultQuickUseSlotCount);
        for (int i = 0; i < quickUseSlotCount; i++)
        {
            quickUseSlots.Add(new InventorySlot());
        }

        for (int i = 0; i < quickUseSlotUis.Count; i++)
        {
            quickUseSlotUis[i].Configure(this, InventorySlotKind.QuickUse, i);
        }

        SyncSpecialSlots();
        RefreshAllSlots();

        if (inventorySlotUis.Count == 0)
        {
            Debug.LogWarning("InventoryUI did not find inventory slots. Put backpack slots under a 'Right', 'Right Panel', or 'ItemsPanel' parent, or assign them in the Inspector.", this);
        }

        if (weaponSlotUis.Count == 0)
        {
            Debug.LogWarning("InventoryUI did not find weapon slots. Put weapon UI slots under a 'Guns' parent or assign them in the Inspector.", this);
        }

        if (quickUseSlotUis.Count == 0)
        {
            Debug.LogWarning("InventoryUI did not find quick-use slots. Put quick item UI slots under an 'Items' parent or assign them in the Inspector.", this);
        }
    }

    private void FindSlotsInPanel()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        Image[] images = inventoryPanel.GetComponentsInChildren<Image>(true);
        List<InventorySlotUI> foundInventorySlots = new List<InventorySlotUI>();
        List<InventorySlotUI> foundWeaponSlots = new List<InventorySlotUI>();
        List<InventorySlotUI> foundQuickUseSlots = new List<InventorySlotUI>();
        List<InventorySlotUI> fallbackInventorySlots = new List<InventorySlotUI>();
        HashSet<InventorySlotUI> foundSlots = new HashSet<InventorySlotUI>();

        foreach (Image image in images)
        {
            if (!IsLikelySlot(image.gameObject))
            {
                continue;
            }

            InventorySlotUI slotUi = image.GetComponent<InventorySlotUI>();
            if (slotUi == null)
            {
                slotUi = image.gameObject.AddComponent<InventorySlotUI>();
            }

            if (!foundSlots.Add(slotUi))
            {
                continue;
            }

            if (IsWeaponSlotUi(image.transform))
            {
                foundWeaponSlots.Add(slotUi);
            }
            else if (IsQuickUseSlotUi(image.transform))
            {
                foundQuickUseSlots.Add(slotUi);
            }
            else if (IsInventorySlotUi(image.transform))
            {
                foundInventorySlots.Add(slotUi);
            }
            else
            {
                fallbackInventorySlots.Add(slotUi);
            }
        }

        if (weaponSlotUis.Count == 0)
        {
            weaponSlotUis.AddRange(foundWeaponSlots);
        }

        if (quickUseSlotUis.Count == 0)
        {
            quickUseSlotUis.AddRange(foundQuickUseSlots);
        }

        if (inventorySlotUis.Count == 0)
        {
            if (foundInventorySlots.Count > 0)
            {
                inventorySlotUis.AddRange(foundInventorySlots);
            }
            else
            {
                inventorySlotUis.AddRange(fallbackInventorySlots);
            }
        }
    }

    private void RefreshAllSlots()
    {
        for (int i = 0; i < inventorySlotUis.Count; i++)
        {
            inventorySlotUis[i].Refresh(inventorySlots[i], false);
        }

        for (int i = 0; i < weaponSlotUis.Count; i++)
        {
            bool isActiveWeapon = weaponEquipment != null && weaponEquipment.IsActiveWeaponSlot(i);
            weaponSlotUis[i].Refresh(weaponSlots[i], isActiveWeapon);
        }

        for (int i = 0; i < quickUseSlotUis.Count; i++)
        {
            quickUseSlotUis[i].Refresh(quickUseSlots[i], false);
        }
    }

    private bool UseConsumable(InventorySlotKind slotKind, int slotIndex, InventoryItemData item)
    {
        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        if (playerStats == null)
        {
            Debug.LogWarning("Cannot use " + item.ItemName + " because PlayerStats was not found.", this);
            return false;
        }

        if (item.HealAmount > 0f)
        {
            playerStats.Heal(item.HealAmount);
        }

        RemoveFromSlot(slotKind, slotIndex, 1);
        return true;
    }

    private bool EquipWeaponFromSlot(InventorySlotKind slotKind, int slotIndex)
    {
        if (slotKind == InventorySlotKind.Weapon)
        {
            return SelectWeaponSlot(slotIndex);
        }

        return MoveWeaponToFirstAvailableQuickSlot(slotKind, slotIndex);
    }

    private bool MoveWeaponToFirstAvailableQuickSlot(InventorySlotKind fromKind, int fromIndex)
    {
        int targetWeaponSlotIndex = FindEmptySlot(weaponSlots);
        if (targetWeaponSlotIndex < 0)
        {
            targetWeaponSlotIndex = 0;
        }

        return MoveSlot(fromKind, fromIndex, InventorySlotKind.Weapon, targetWeaponSlotIndex);
    }

    private void SyncSpecialSlots()
    {
        SyncWeaponSlotsToEquipment();
    }

    private void SyncWeaponSlotsToEquipment()
    {
        ResolveWeaponEquipment();

        if (weaponEquipment == null)
        {
            return;
        }

        for (int i = 0; i < weaponEquipment.SlotCount; i++)
        {
            InventoryItemData weaponItem = i < weaponSlots.Count && !weaponSlots[i].IsEmpty ? weaponSlots[i].Item : null;
            weaponEquipment.SetWeaponSlotItem(i, weaponItem);
        }
    }

    private void RemoveFromSlot(InventorySlotKind slotKind, int slotIndex, int amount)
    {
        if (!IsValidSlotIndex(slotKind, slotIndex))
        {
            return;
        }

        GetSlot(slotKind, slotIndex).Remove(amount);

        if (slotKind == InventorySlotKind.Weapon)
        {
            SyncWeaponSlotsToEquipment();
        }

        RefreshAllSlots();
    }

    private Vector3 GetDropPosition()
    {
        Transform dropRoot = null;
        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            dropRoot = playerController.transform;
        }
        else if (Camera.main != null)
        {
            dropRoot = Camera.main.transform;
        }

        if (dropRoot == null)
        {
            return Vector3.zero;
        }

        return dropRoot.position + dropRoot.forward * 1.25f + Vector3.up * 0.25f;
    }

    private bool CreateDropPickup(InventoryItemData item, int amount, Vector3 position)
    {
        if (item == null || amount <= 0)
        {
            return false;
        }

        GameObject prefab = item.DropPrefab;
        if (prefab == null)
        {
            Debug.LogWarning("Cannot drop item because no drop prefab is assigned.", this);
            return false;
        }

        GameObject dropObject = Instantiate(prefab, position, Quaternion.identity);
        EnablePhysics(dropObject);

        PickupItem pickup = dropObject.GetComponent<PickupItem>();
        if (pickup == null)
        {
            pickup = dropObject.AddComponent<PickupItem>();
        }

        pickup.Setup(item, amount, true);
        return true;
    }

    private void EnablePhysics(GameObject dropObject)
    {
        if (dropObject == null)
        {
            return;
        }

        Rigidbody[] bodies = dropObject.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in bodies)
        {
            rb.isKinematic = false;
            rb.detectCollisions = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void ShowItemDescription(InventoryItemData item, int amount)
    {
        ResolveDescriptionReferences();

        if (item == null)
        {
            ClearDescription();
            return;
        }

        string title = item.ItemName;
        string body = BuildDescriptionBody(item, amount);

        if (descriptionTitleText != null)
        {
            descriptionTitleText.text = title;
        }

        if (descriptionBodyText != null)
        {
            descriptionBodyText.textWrappingMode = TextWrappingModes.Normal;
            descriptionBodyText.text = body;
        }
        else if (descriptionTitleText != null)
        {
            descriptionTitleText.text = title + Environment.NewLine + body;
        }
    }

    private void ClearDescription()
    {
        ResolveDescriptionReferences();

        if (descriptionTitleText != null)
        {
            descriptionTitleText.text = "Select item";
        }

        if (descriptionBodyText != null)
        {
            descriptionBodyText.text = "Left click an item to inspect it.";
        }
    }

    private string BuildDescriptionBody(InventoryItemData item, int amount)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("Type: " + item.ItemType);

        if (amount > 1)
        {
            builder.AppendLine("Amount: " + amount);
        }

        if (!string.IsNullOrWhiteSpace(item.Description))
        {
            builder.AppendLine();
            builder.AppendLine(item.Description);
        }

        switch (item.ItemType)
        {
            case InventoryItemType.Consumable:
                builder.AppendLine();
                builder.AppendLine("Heal: " + item.HealAmount);
                break;
            case InventoryItemType.Weapon:
                builder.AppendLine();
                builder.AppendLine("Damage: " + item.WeaponDamage);
                builder.AppendLine("Range: " + item.WeaponRange);
                builder.AppendLine("Fire rate: " + item.WeaponFireRate);
                break;
            case InventoryItemType.Key:
                builder.AppendLine();
                builder.AppendLine("Key ID: " + item.KeyId);
                break;
            default:
                if (item.AmoDamage > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine("Ammo DMG: " + item.AmoDamage);
                }
                break;
        }

        return builder.ToString().TrimEnd();
    }

    private bool ContainsKey(List<InventorySlot> slotList, string keyId)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (SlotContainsKey(slotList[i], keyId))
            {
                return true;
            }
        }

        return false;
    }

    private bool ConsumeKeyFromSlots(List<InventorySlot> slotList, string keyId)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (!SlotContainsKey(slotList[i], keyId))
            {
                continue;
            }

            return slotList[i].Remove(1);
        }

        return false;
    }

    private bool SlotContainsKey(InventorySlot slot, string keyId)
    {
        return slot != null &&
               !slot.IsEmpty &&
               slot.Item.ItemType == InventoryItemType.Key &&
               string.Equals(slot.Item.KeyId, keyId, StringComparison.OrdinalIgnoreCase);
    }

    private int FindStackSlot(List<InventorySlot> slotList, InventoryItemData item)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (slotList[i].CanStack(item))
            {
                return i;
            }
        }

        return -1;
    }

    private int FindEmptySlot(List<InventorySlot> slotList)
    {
        for (int i = 0; i < slotList.Count; i++)
        {
            if (slotList[i].IsEmpty)
            {
                return i;
            }
        }

        return -1;
    }

    private void EnsureSlotsBuilt()
    {
        if (inventorySlots.Count == 0)
        {
            BuildSlots();
        }
    }

    private bool IsValidSlotIndex(InventorySlotKind slotKind, int slotIndex)
    {
        List<InventorySlot> slotList = GetSlotList(slotKind);
        return slotIndex >= 0 && slotIndex < slotList.Count;
    }

    private List<InventorySlot> GetSlotList(InventorySlotKind slotKind)
    {
        switch (slotKind)
        {
            case InventorySlotKind.Weapon:
                return weaponSlots;
            case InventorySlotKind.QuickUse:
                return quickUseSlots;
            default:
                return inventorySlots;
        }
    }

    private int GetWeaponSlotCount()
    {
        ResolveWeaponEquipment();
        return weaponEquipment != null ? weaponEquipment.SlotCount : 3;
    }

    private bool CanPlaceItemInSlotKind(InventorySlotKind slotKind, InventoryItemData item)
    {
        if (item == null || slotKind == InventorySlotKind.Inventory)
        {
            return true;
        }

        if (slotKind == InventorySlotKind.Weapon)
        {
            return item.ItemType == InventoryItemType.Weapon;
        }

        if (slotKind == InventorySlotKind.QuickUse)
        {
            return item.ItemType == InventoryItemType.Consumable;
        }

        return true;
    }

    private string GetRejectedSlotMessage(InventorySlotKind slotKind)
    {
        switch (slotKind)
        {
            case InventorySlotKind.Weapon:
                return "Only weapons can go into weapon slots.";
            case InventorySlotKind.QuickUse:
                return "Only consumable healing items can go into quick-use slots.";
            default:
                return "This item cannot go into that slot.";
        }
    }

    private void SwapSlotContents(InventorySlot firstSlot, InventorySlot secondSlot)
    {
        InventoryItemData tempItem = secondSlot.Item;
        int tempAmount = secondSlot.Amount;

        if (firstSlot.IsEmpty)
        {
            secondSlot.Clear();
        }
        else
        {
            secondSlot.Set(firstSlot.Item, firstSlot.Amount);
        }

        if (tempItem == null)
        {
            firstSlot.Clear();
        }
        else
        {
            firstSlot.Set(tempItem, tempAmount);
        }
    }

    private void ResolveReferences()
    {
        if (inventoryPanel == null)
        {
            inventoryPanel = gameObject;
        }

        if (playerStats == null)
        {
            playerStats = FindFirstObjectByType<PlayerStats>();
        }

        ResolveWeaponEquipment();
        ResolveDescriptionReferences();
    }

    private void ResolveWeaponEquipment()
    {
        if (weaponEquipment != null)
        {
            return;
        }

        weaponEquipment = FindFirstObjectByType<WeaponEquipment>();

        if (weaponEquipment == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
            {
                weaponEquipment = playerController.GetComponent<WeaponEquipment>();

                if (weaponEquipment == null)
                {
                    weaponEquipment = playerController.gameObject.AddComponent<WeaponEquipment>();
                }
            }
        }
    }

    private void ResolveDescriptionReferences()
    {
        if (descriptionPanel == null && inventoryPanel != null)
        {
            Transform[] transforms = inventoryPanel.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in transforms)
            {
                if (string.Equals(child.name, "Description", StringComparison.OrdinalIgnoreCase))
                {
                    descriptionPanel = child.gameObject;
                    break;
                }
            }
        }

        if (descriptionPanel == null)
        {
            return;
        }

        TextMeshProUGUI[] texts = descriptionPanel.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (descriptionTitleText == null && texts.Length > 0)
        {
            descriptionTitleText = texts[0];
        }

        if (descriptionBodyText == null && texts.Length > 1)
        {
            descriptionBodyText = texts[1];
        }
    }

    private void EnsureEventSystem()
    {
        if (!createEventSystemIfMissing || EventSystem.current != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private static bool IsLikelySlot(GameObject gameObject)
    {
        return gameObject.name.IndexOf("slot", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsWeaponSlotUi(Transform transform)
    {
        return HasAncestorNamed(transform, "Guns") || HasAncestorNamed(transform, "Weapons");
    }

    private static bool IsQuickUseSlotUi(Transform transform)
    {
        return HasAncestorNamed(transform, "Items") ||
               HasAncestorNamed(transform, "QuickUse") ||
               HasAncestorNamed(transform, "Quick Items") ||
               HasAncestorNamed(transform, "Consumables");
    }

    private static bool IsInventorySlotUi(Transform transform)
    {
        return HasAncestorNamed(transform, "Right") ||
               HasAncestorNamed(transform, "Right Panel") ||
               HasAncestorNamed(transform, "ItemsPanel") ||
               HasAncestorNamed(transform, "Backpack");
    }

    private static bool HasAncestorNamed(Transform transform, string ancestorName)
    {
        Transform current = transform;

        while (current != null)
        {
            if (string.Equals(current.name, ancestorName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}
