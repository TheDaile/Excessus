using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class InventoryItem : ScriptableObject
{
    public int id;

    public string itemName;

    public ItemType itemType;

    public Sprite icon;

    public GameObject worldPrefab;
}