using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private int inventorySize = 24;

    public List<InventorySlot> slots = new();

    private void Awake()
    {
        for (int i = 0; i < inventorySize; i++)
        {
            slots.Add(new InventorySlot());
        }
    }
}