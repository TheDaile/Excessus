using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image background;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;
    [SerializeField] private TextMeshProUGUI fallbackIconText;
    [SerializeField] private Color normalColor = new Color(0.09f, 0.09f, 0.09f, 1f);
    [SerializeField] private Color equippedColor = new Color(0.17f, 0.42f, 0.28f, 1f);

    private static InventorySlotUI draggedSlot;
    private static GameObject dragGhost;

    private InventoryUI owner;
    private InventorySlotKind slotKind;
    private int slotIndex = -1;
    private InventorySlot currentSlot;

    public InventorySlotKind SlotKind => slotKind;
    public int SlotIndex => slotIndex;
    private bool HasItem => currentSlot != null && !currentSlot.IsEmpty;

    public void Configure(InventoryUI inventoryOwner, InventorySlotKind kind, int index)
    {
        owner = inventoryOwner;
        slotKind = kind;
        slotIndex = index;
        EnsureVisuals();
    }

    public void Configure(InventoryUI inventoryOwner, int index)
    {
        Configure(inventoryOwner, InventorySlotKind.Inventory, index);
    }

    public void Refresh(InventorySlot slot, bool isEquipped)
    {
        EnsureVisuals();

        currentSlot = slot;
        background.color = isEquipped ? equippedColor : normalColor;

        if (slot == null || slot.IsEmpty)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            fallbackIconText.text = string.Empty;
            amountText.text = string.Empty;
            return;
        }

        iconImage.sprite = slot.Item.Icon;
        iconImage.enabled = slot.Item.Icon != null;
        fallbackIconText.text = slot.Item.Icon == null ? GetFallbackIcon(slot.Item) : string.Empty;
        amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : string.Empty;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (owner == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            owner.ShowSlotDescription(slotKind, slotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            owner.UseSlot(slotKind, slotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Middle)
        {
            owner.DropSlotItem(slotKind, slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!HasItem)
        {
            return;
        }

        CreateDragGhost(eventData);
        draggedSlot = this;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragGhost != null)
        {
            dragGhost.transform.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        bool droppedOutside = draggedSlot == this && !IsPointerOverInventorySlot(eventData);
        if (droppedOutside && owner != null)
        {
            owner.DropSlotItem(slotKind, slotIndex);
        }

        ClearDragGhost();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (owner == null || draggedSlot == null || draggedSlot == this)
        {
            return;
        }

        owner.MoveSlot(draggedSlot.SlotKind, draggedSlot.SlotIndex, slotKind, slotIndex);
    }

    private bool IsPointerOverInventorySlot(PointerEventData eventData)
    {
        if (eventData == null || eventData.pointerCurrentRaycast.gameObject == null)
        {
            return false;
        }

        InventorySlotUI slotUi = eventData.pointerCurrentRaycast.gameObject.GetComponentInParent<InventorySlotUI>();
        return slotUi != null;
    }

    private void EnsureVisuals()
    {
        if (background == null)
        {
            background = GetComponent<Image>();
        }

        if (background == null)
        {
            background = gameObject.AddComponent<Image>();
        }

        background.raycastTarget = true;

        if (iconImage == null)
        {
            iconImage = FindChildImage("Icon");
        }

        if (iconImage == null)
        {
            iconImage = CreateChildImage("Icon", 12f);
        }

        iconImage.raycastTarget = false;

        if (fallbackIconText == null)
        {
            fallbackIconText = FindChildText("Fallback Icon");
        }

        if (fallbackIconText == null)
        {
            fallbackIconText = CreateChildText("Fallback Icon", 0f, TextAlignmentOptions.Center, 32f);
        }

        fallbackIconText.raycastTarget = false;

        if (amountText == null)
        {
            amountText = FindChildText("Amount");
        }

        if (amountText == null)
        {
            amountText = CreateChildText("Amount", 6f, TextAlignmentOptions.BottomRight, 18f);
        }

        amountText.raycastTarget = false;
    }

    private Image FindChildImage(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<Image>();
    }

    private TextMeshProUGUI FindChildText(string childName)
    {
        Transform child = transform.Find(childName);
        return child == null ? null : child.GetComponent<TextMeshProUGUI>();
    }

    private Image CreateChildImage(string childName, float padding)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        RectTransform rectTransform = child.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.one * padding;
        rectTransform.offsetMax = Vector2.one * -padding;

        Image image = child.AddComponent<Image>();
        image.preserveAspect = true;
        image.enabled = false;
        return image;
    }

    private TextMeshProUGUI CreateChildText(string childName, float padding, TextAlignmentOptions alignment, float fontSize)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);

        RectTransform rectTransform = child.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.one * padding;
        rectTransform.offsetMax = Vector2.one * -padding;

        TextMeshProUGUI text = child.AddComponent<TextMeshProUGUI>();
        text.alignment = alignment;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.text = string.Empty;
        return text;
    }

    private void CreateDragGhost(PointerEventData eventData)
    {
        DestroyDragGhost();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        dragGhost = new GameObject("Dragged Inventory Item");
        dragGhost.transform.SetParent(canvas.transform, false);

        RectTransform sourceRect = GetComponent<RectTransform>();
        RectTransform ghostRect = dragGhost.AddComponent<RectTransform>();
        ghostRect.sizeDelta = sourceRect.rect.size;

        CanvasGroup canvasGroup = dragGhost.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;

        Image ghostImage = dragGhost.AddComponent<Image>();
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;
        ghostImage.sprite = currentSlot.Item.Icon;
        ghostImage.color = currentSlot.Item.Icon == null ? new Color(0.2f, 0.2f, 0.2f, 0.85f) : Color.white;

        if (currentSlot.Item.Icon == null)
        {
            TextMeshProUGUI ghostText = CreateGhostText(dragGhost.transform);
            ghostText.text = GetFallbackIcon(currentSlot.Item);
        }

        dragGhost.transform.position = eventData.position;
    }

    private TextMeshProUGUI CreateGhostText(Transform parent)
    {
        GameObject child = new GameObject("Fallback Icon");
        child.transform.SetParent(parent, false);

        RectTransform rectTransform = child.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        TextMeshProUGUI text = child.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 32f;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static void ClearDragGhost()
    {
        DestroyDragGhost();
        draggedSlot = null;
    }

    private static void DestroyDragGhost()
    {
        if (dragGhost != null)
        {
            Destroy(dragGhost);
            dragGhost = null;
        }
    }

    private static string GetFallbackIcon(InventoryItemData item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.ItemName))
        {
            return "?";
        }

        return item.ItemName.Substring(0, 1).ToUpperInvariant();
    }
}
