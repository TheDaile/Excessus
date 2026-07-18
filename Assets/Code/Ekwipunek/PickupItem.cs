using UnityEngine;

public class PickupItem : Interactable
{
    [SerializeField] private InventoryItemData item;
    [Min(1)]
    [SerializeField] private int amount = 1;
    [SerializeField] private bool destroyWhenPickedUp = true;

    private InventoryUI inventory;

    private void Awake()
    {
        RefreshPrompt();
    }

    protected override void Interact()
    {
        if (item == null)
        {
            Debug.LogWarning("PickupItem has no item assigned.", this);
            return;
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<InventoryUI>();
        }

        if (inventory == null)
        {
            Debug.LogWarning("No InventoryUI found in the scene.", this);
            return;
        }

        int addedAmount = inventory.AddItem(item, amount);

        if (addedAmount <= 0)
        {
            Debug.Log("Inventory is full.");
            return;
        }

        amount -= addedAmount;

        if (amount <= 0)
        {
            if (destroyWhenPickedUp)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
        else
        {
            RefreshPrompt();
        }
    }

    private void RefreshPrompt()
    {
        if (item == null)
        {
            promptMessage = "pick up item";
            return;
        }

        promptMessage = "pick up " + item.ItemName;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);
        RefreshPrompt();
    }
#endif
}
