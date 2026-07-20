using System;
using System.Runtime.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory Item", menuName = "Excessus/Inventory/Item")]
public class InventoryItemData : ScriptableObject
{
    [Header("Item")]
    [SerializeField] private string itemName = "New Item";
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private InventoryItemType itemType = InventoryItemType.Generic;
    [SerializeField] private bool stackable = true;
    [Min(1)]
    [SerializeField] private int pickupAmount = 1;

    [Header("Consumable")]
    [Min(0f)]
    [SerializeField] private float healAmount;

    [Header("Weapon")]
    [SerializeField] private GameObject weapon;
    [Min(0f)]
    [SerializeField] private float weaponDamage = 10f;
    [Min(0f)]
    [SerializeField] private float weaponRange = 100f;
    [Min(0f)]
    [SerializeField] private float weaponFireRate = 15f;
    [Min(0f)]
    [SerializeField] private float weaponFireForce = 15f;

    [Header("Drop")]
    [SerializeField] private GameObject dropPrefab;

    [Header("Key")]
    [SerializeField] private string keyId = "default";

    [Header("Plot")]
    [SerializeField] private string PlotId = "default";

    [Header("Amunition")]
    [SerializeField] private float AmoDMG = 10f;
    [Min(0f)]
    [SerializeField] private string AmoId = "default";


    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => icon;
    public InventoryItemType ItemType => itemType;
    public bool IsStackable => itemType != InventoryItemType.Weapon && stackable;
    public int PickupAmount => pickupAmount;
    public float HealAmount => healAmount;
    public float WeaponDamage => weaponDamage;
    public float WeaponRange => weaponRange;
    public float WeaponFireRate => weaponFireRate;
    public float WeaponFireForce => weaponFireForce;
    public string KeyId => keyId;
    public GameObject Weapon => weapon;
    public GameObject DropPrefab => dropPrefab != null ? dropPrefab : weapon;
    public float AmoDamage => AmoDMG;
    public string AmoItemId => AmoId;

#if UNITY_EDITOR
    private void OnValidate()
    {
        pickupAmount = Mathf.Max(1, pickupAmount);

        if (itemType == InventoryItemType.Weapon)
        {
            stackable = false;
        }
    }
#endif
}
