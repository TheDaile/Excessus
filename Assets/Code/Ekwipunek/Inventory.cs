using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject inventoryPanel;

    private bool isOpen = false;

    public bool IsOpen => isOpen;

    private void Start()
    {
        inventoryPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleInventory();
        }
    }

    public void ToggleInventory()
    {
        isOpen = !isOpen;

        inventoryPanel.SetActive(isOpen);

        Cursor.visible = isOpen;

        Cursor.lockState = isOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;

        Time.timeScale = 1f;
    }
}