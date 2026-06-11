using UnityEngine;

public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField]private float distance = 3f;
    [SerializeField] private LayerMask mask;
    [SerializeField] private PlayerHUD playerHUD;
    private Interactable currentInteractable;

    void Awake()
    {
        if (playerHUD == null)
        {
            throw new MissingReferenceException("PlayerInteract requires a PlayerHUD reference. Assign it in the Inspector.");
        }

        PlayerLook playerLook = GetComponent<PlayerLook>();

        if (playerLook == null)
        {
            throw new MissingComponentException("PlayerInteract requires PlayerLook on the same GameObject.");
        }

        cam = playerLook.playerCamera;

        if (cam == null)
        {
            throw new MissingReferenceException("PlayerInteract requires PlayerLook.playerCamera to be assigned.");
        }
    }
    public void CheckForInteractable()
    {
        currentInteractable = null;
        playerHUD.UpdateText(string.Empty);
        
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        RaycastHit hitInfo;
        if(Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            if(hitInfo.collider.TryGetComponent<Interactable>(out Interactable interactable))
            {
                currentInteractable = interactable;
                playerHUD.UpdateText("Press E to " + interactable.promptMessage);
            }
        }
    }

    public void Interact()
    {
        if (currentInteractable == null)
        {
            return;
        }

        currentInteractable.BaseInteract();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (playerHUD == null)
        {
            Debug.LogWarning("PlayerInteract: PlayerHUD reference is not assigned.", this);
        }

        if (mask.value == 0)
        {
            Debug.LogWarning("PlayerInteract: No layer mask selected in the Inspector.", this);
        }
    }
#endif
}