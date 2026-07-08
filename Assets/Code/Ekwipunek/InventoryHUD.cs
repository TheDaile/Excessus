using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class InventoryHUD : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement inventoryRoot;

    private PlayerInput playerInput;
    private InputAction inventoryAction;

    private bool isOpen;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        inventoryRoot = uiDocument.rootVisualElement.Q<VisualElement>("InventoryRoot");
        inventoryRoot.style.display = DisplayStyle.None;

        playerInput = GetComponent<PlayerInput>();

        inventoryAction = playerInput.currentActionMap["Inventory"];
    }

    private void OnEnable()
    {
        inventoryAction.performed += ToggleInventory;
    }

    private void OnDisable()
    {
        inventoryAction.performed -= ToggleInventory;
    }

    private void ToggleInventory(InputAction.CallbackContext context)
    {
        isOpen = !isOpen;

        inventoryRoot.style.display =
            isOpen ? DisplayStyle.Flex : DisplayStyle.None;
    }
}