using System;
using UnityEngine;

public class WeaponEquipment : MonoBehaviour
{
    private const int DefaultWeaponSlotCount = 3;

    [SerializeField] private Camera weaponCamera;
    [SerializeField] private bool autoFindWeaponObjects = true;
    [SerializeField] private GameObject[] weaponObjects = new GameObject[DefaultWeaponSlotCount];
    // Tracks instantiated weapon instance (the actual prefab instance) for each slot
    private GameObject[] weaponInstanceObjects = new GameObject[DefaultWeaponSlotCount];

    private readonly InventoryItemData[] weaponItems = new InventoryItemData[DefaultWeaponSlotCount];
    private readonly Gun[] weaponGuns = new Gun[DefaultWeaponSlotCount];
    private int activeSlotIndex = -1;

    public int SlotCount => weaponItems.Length;
    public int ActiveSlotIndex => activeSlotIndex;
    public bool HasAnyAssignedWeapon => FindFirstAssignedWeaponSlot() >= 0;
    public Gun ActiveGun => IsValidSlotIndex(activeSlotIndex) ? weaponGuns[activeSlotIndex] : null;

    private void Awake()
    {
        ResolveReferences();
        ApplyActiveSlot();
    }

    public void SetWeaponSlotItem(int slotIndex, InventoryItemData item)
    {
        if (!IsValidSlotIndex(slotIndex))
        {
            return;
        }

        if (item != null && item.ItemType != InventoryItemType.Weapon)
        {
            Debug.LogWarning("Only weapon items can be assigned to weapon slots.", this);
            return;
        }

        // Assign logical item
        weaponItems[slotIndex] = item;

        // Parent (slot) object if present
        GameObject slotParent = weaponObjects[slotIndex];
        GameObject existingInstance = weaponInstanceObjects[slotIndex];

        if (item != null)
        {
            // Destroy any previous instance created for this slot
            if (existingInstance != null)
            {
                Destroy(existingInstance);
                weaponInstanceObjects[slotIndex] = null;
            }

            // If a parent slot exists, instantiate the weapon prefab as its child
            if (slotParent != null)
            {
                if (item.Weapon != null)
                {
                    GameObject instantiated = Instantiate(item.Weapon, slotParent.transform);
                    instantiated.name = item.Weapon.name;
                    weaponInstanceObjects[slotIndex] = instantiated;
                }
            }
            else
            {
                // No parent slot - create one under the weapon camera and use it as parent
                if (item.Weapon != null)
                {
                    Transform parent = weaponCamera != null ? weaponCamera.transform : transform;
                    GameObject slotObj = new GameObject(slotIndex == 0 ? "Gun Slot" : $"Gun Slot_{slotIndex}");
                    slotObj.transform.SetParent(parent, false);
                    weaponObjects[slotIndex] = slotObj;

                    GameObject instantiated = Instantiate(item.Weapon, slotObj.transform);
                    instantiated.name = item.Weapon.name;
                    weaponInstanceObjects[slotIndex] = instantiated;
                }
            }

            // Ensure gun reference is resolved (will find Gun in children)
            ResolveGun(slotIndex);
        }
        else
        {
            // item == null, clear any instantiated instance for this slot
            if (existingInstance != null)
            {
                Destroy(existingInstance);
                weaponInstanceObjects[slotIndex] = null;
            }
        }

        if (item == null && activeSlotIndex == slotIndex)
        {
            activeSlotIndex = FindFirstAssignedWeaponSlot();
        }
        else if (item != null && activeSlotIndex < 0)
        {
            activeSlotIndex = slotIndex;
        }

        ApplyActiveSlot();
    }

    public InventoryItemData GetWeaponSlotItem(int slotIndex)
    {
        return IsValidSlotIndex(slotIndex) ? weaponItems[slotIndex] : null;
    }

    public bool SelectWeapon(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex) || weaponItems[slotIndex] == null)
        {
            return false;
        }

        activeSlotIndex = slotIndex;
        ApplyActiveSlot();
        return true;
    }

    public void FireActiveWeapon()
    {
        ActiveGun?.FireRate();
    }

    public bool IsActiveWeaponSlot(int slotIndex)
    {
        return slotIndex == activeSlotIndex;
    }

    private void ResolveReferences()
    {
        if (weaponCamera == null)
        {
            weaponCamera = GetComponentInChildren<Camera>(true);
        }

        if (weaponCamera == null)
        {
            weaponCamera = Camera.main;
        }

        EnsureWeaponObjectArray();

        if (autoFindWeaponObjects)
        {
            AutoFindWeaponObjects();
        }

        for (int i = 0; i < weaponObjects.Length; i++)
        {
            ResolveGun(i);
        }
    }

    private void EnsureWeaponObjectArray()
    {
        if (weaponObjects == null)
        {
            weaponObjects = new GameObject[DefaultWeaponSlotCount];
            return;
        }

        if (weaponObjects.Length != DefaultWeaponSlotCount)
        {
            Array.Resize(ref weaponObjects, DefaultWeaponSlotCount);
            Array.Resize(ref weaponInstanceObjects, DefaultWeaponSlotCount);
        }
    }

    private void AutoFindWeaponObjects()
    {
        Transform searchRoot = weaponCamera != null ? weaponCamera.transform : transform;
        Transform[] children = searchRoot.GetComponentsInChildren<Transform>(true);

        foreach (Transform child in children)
        {
            int slotIndex = GetAutoWeaponSlotIndex(child.name);
            if (!IsValidSlotIndex(slotIndex) || weaponObjects[slotIndex] != null)
            {
                continue;
            }

            weaponObjects[slotIndex] = child.gameObject;
            // no instance created by system yet for this parent
            if (weaponInstanceObjects.Length > slotIndex)
            {
                weaponInstanceObjects[slotIndex] = null;
            }
        }
    }

    private void ResolveGun(int slotIndex)
    {
        if (!IsValidSlotIndex(slotIndex) || weaponObjects[slotIndex] == null)
        {
            weaponGuns[slotIndex] = null;
            return;
        }

        weaponGuns[slotIndex] = weaponObjects[slotIndex].GetComponentInChildren<Gun>(true);

        if (weaponGuns[slotIndex] != null && weaponGuns[slotIndex].fpsCam == null)
        {
            weaponGuns[slotIndex].fpsCam = weaponCamera;
        }
    }

    private void ApplyActiveSlot()
    {
        ResolveReferencesIfNeeded();

        for (int i = 0; i < weaponObjects.Length; i++)
        {
            bool shouldBeActive = i == activeSlotIndex && weaponItems[i] != null;

            if (weaponObjects[i] != null)
            {
                weaponObjects[i].SetActive(shouldBeActive);
            }

            if (weaponGuns[i] == null)
            {
                continue;
            }

            if (shouldBeActive)
            {
                weaponGuns[i].Equip(weaponItems[i]);
            }
            else
            {
                weaponGuns[i].Unequip();
            }
        }
    }

    private void ResolveReferencesIfNeeded()
    {
        for (int i = 0; i < weaponObjects.Length; i++)
        {
            if (weaponObjects[i] != null && weaponGuns[i] == null)
            {
                ResolveGun(i);
            }
        }
    }

    private int FindFirstAssignedWeaponSlot()
    {
        for (int i = 0; i < weaponItems.Length; i++)
        {
            if (weaponItems[i] != null)
            {
                return i;
            }
        }

        return -1;
    }

    private bool IsValidSlotIndex(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < weaponItems.Length;
    }

    private static int GetAutoWeaponSlotIndex(string objectName)
    {
        if (string.Equals(objectName, "Gun Slot", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        const string prefix = "Gun Slot_";
        if (!objectName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        string suffix = objectName.Substring(prefix.Length);
        return int.TryParse(suffix, out int parsedIndex) ? parsedIndex : -1;
    }
}
