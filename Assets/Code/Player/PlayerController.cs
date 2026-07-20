using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerLook))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteract))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject inventoryWindow;
    [SerializeField] private InventoryUI inventory;
    [SerializeField] private WeaponEquipment weaponEquipment;
    [SerializeField] private float Force;

    private PlayerInput playerInput;
    private PlayerInput.PlayerActions playerActions;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private PlayerInteract playerInteract;
    private Gun gun;

    private bool inventoryOpen;

    void Awake()
    {
        gun = GetComponentInChildren<Gun>();

        playerInput = new PlayerInput();
        playerActions = playerInput.Player;


        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
        playerInteract = GetComponent<PlayerInteract>();

        ResolveWeaponEquipment();
        ResolveInventory();

        playerActions.Inventory.performed += ctx => ToggleInventory();

        playerActions.Jump.performed += ctx => playerMovement.Jump();
        playerActions.Interact.performed += ctx => playerInteract.Interact();

        playerActions.Sprint.performed += ctx =>
        {
            if (PlayerSettings.SprintMode == ButtonMode.Toggle)
            {
                playerMovement.ToggleSprint();
            }
            else
            {

                playerMovement.SetSprinting(true);
            }
        };
        playerActions.Sprint.canceled += ctx =>
        {
            if (PlayerSettings.SprintMode == ButtonMode.Hold)
            {
                playerMovement.SetSprinting(false);
            }
        };
        playerActions.Crouch.performed += ctx =>
        {
            if (PlayerSettings.CrouchMode == ButtonMode.Toggle)
            {
                playerMovement.ToggleCrouch();
            }
            else
            {
                playerMovement.SetCrouching(ctx.ReadValueAsButton());
            }
        };
        playerActions.Crouch.canceled += ctx =>
        {
            if (PlayerSettings.CrouchMode == ButtonMode.Hold)
            {
                playerMovement.SetCrouching(false);
            }
        };

    }

    private void Update()
    {
        inventoryOpen = inventory != null ? inventory.IsOpen : inventoryOpen;

        if (inventoryOpen)
        {
            return;
        }
        playerMovement.ProcessMove(playerActions.Move.ReadValue<Vector2>());
        playerLook.ProcessLook(playerActions.Look.ReadValue<Vector2>());
        playerInteract.CheckForInteractable();
        HandleWeaponHotkeys();
        HandleQuickUseHotkeys();

        if (playerActions.Attack.ReadValue<float>() > 0f)
        {
            if (weaponEquipment != null && weaponEquipment.HasAnyAssignedWeapon)
            {
                weaponEquipment.FireActiveWeapon();
            }
            else
            {
                gun?.FireRate();
            }
        }

    }

    private void ToggleInventory()
    {
        if (inventory != null)
        {
            inventory.ToggleInventory();
            inventoryOpen = inventory.IsOpen;
            return;
        }

        inventoryOpen = !inventoryOpen;

        if (inventoryWindow != null)
        {
            inventoryWindow.SetActive(inventoryOpen);
        }

        Cursor.visible = inventoryOpen;

        Cursor.lockState = inventoryOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;
    }

    private void ResolveInventory()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<InventoryUI>();
        }

        if (inventory != null)
        {
            inventory.SetOpen(false);
            inventoryOpen = inventory.IsOpen;
            return;
        }

        if (inventoryWindow != null)
        {
            inventoryWindow.SetActive(false);
        }
    }

    private void ResolveWeaponEquipment()
    {
        if (weaponEquipment == null)
        {
            weaponEquipment = GetComponent<WeaponEquipment>();
        }

        if (weaponEquipment == null)
        {
            weaponEquipment = gameObject.AddComponent<WeaponEquipment>();
        }
    }

    private void HandleWeaponHotkeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
        {
            SelectWeaponSlot(0);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
        {
            SelectWeaponSlot(1);
        }
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
        {
            SelectWeaponSlot(2);
        }
    }

    private void SelectWeaponSlot(int slotIndex)
    {
        if (inventory != null && inventory.SelectWeaponSlot(slotIndex))
        {
            return;
        }

        weaponEquipment?.SelectWeapon(slotIndex);
    }

    private void HandleQuickUseHotkeys()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || inventory == null)
        {
            return;
        }

        if (keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame)
        {
            inventory.UseQuickSlot(0);
        }
        else if (keyboard.digit5Key.wasPressedThisFrame || keyboard.numpad5Key.wasPressedThisFrame)
        {
            inventory.UseQuickSlot(1);
        }
        else if (keyboard.digit6Key.wasPressedThisFrame || keyboard.numpad6Key.wasPressedThisFrame)
        {
            inventory.UseQuickSlot(2);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
{
    Rigidbody rb = hit.collider.attachedRigidbody;

    if (rb == null || rb.isKinematic)
        return;

    Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

    rb.AddForce(pushDir * Force, ForceMode.Impulse);
}


    private void OnEnable() => playerActions.Enable();
    private void OnDisable() => playerActions.Disable();


}
