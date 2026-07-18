using System;
using UnityEngine;

[Serializable]
public class InventorySlot
{
    [SerializeField] private InventoryItemData item;
    [SerializeField] private int amount;

    public InventoryItemData Item => item;
    public int Amount => IsEmpty ? 0 : amount;
    public bool IsEmpty => item == null || amount <= 0;

    public bool CanStack(InventoryItemData otherItem)
    {
        return !IsEmpty && item == otherItem && item.IsStackable;
    }

    public void Set(InventoryItemData newItem, int newAmount)
    {
        item = newItem;
        amount = newItem == null ? 0 : Mathf.Max(1, newAmount);
    }

    public void Add(int value)
    {
        if (IsEmpty)
        {
            return;
        }

        amount = Mathf.Max(1, amount + value);
    }

    public bool Remove(int value)
    {
        if (IsEmpty || value <= 0)
        {
            return false;
        }

        amount -= value;

        if (amount <= 0)
        {
            Clear();
        }

        return true;
    }

    public void Clear()
    {
        item = null;
        amount = 0;
    }
}
