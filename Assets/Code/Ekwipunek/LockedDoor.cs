using UnityEngine;

public class LockedDoor : Interactable
{
    [SerializeField] private string requiredKeyId = "default";
    [SerializeField] private bool consumeKey;
    [SerializeField] private Animator animator;
    [SerializeField] private string openTrigger = "Open";
    [SerializeField] private Transform doorTransform;
    [SerializeField] private Vector3 openEulerOffset = new Vector3(0f, 90f, 0f);
    [SerializeField] private bool disableColliderWhenOpened = true;

    private Quaternion closedRotation;
    private bool isOpen;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        if (doorTransform == null)
        {
            doorTransform = transform;
        }

        closedRotation = doorTransform.localRotation;
        promptMessage = "open";
    }

    protected override void Interact()
    {
        if (isOpen)
        {
            return;
        }

        InventoryUI inventory = FindFirstObjectByType<InventoryUI>();
        if (inventory == null)
        {
            Debug.LogWarning("LockedDoor could not find InventoryUI in the scene.", this);
            return;
        }

        if (!inventory.HasKey(requiredKeyId))
        {
            Debug.Log("Door is locked. Missing key: " + requiredKeyId);
            return;
        }

        if (consumeKey)
        {
            inventory.ConsumeKey(requiredKeyId);
        }

        Open();
    }

    public void Open()
    {
        isOpen = true;
        promptMessage = string.Empty;

        if (animator != null && !string.IsNullOrWhiteSpace(openTrigger))
        {
            animator.SetTrigger(openTrigger);
        }
        else if (doorTransform != null)
        {
            doorTransform.localRotation = closedRotation * Quaternion.Euler(openEulerOffset);
        }

        if (disableColliderWhenOpened && TryGetComponent(out Collider doorCollider))
        {
            doorCollider.enabled = false;
        }
    }
}
