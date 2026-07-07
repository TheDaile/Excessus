using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerLook))]
public class PlayerInteract : NetworkBehaviour
{
    private const ulong NoInteractableNetworkObjectId = ulong.MaxValue;

    private NetworkVariable<ulong> currentInteractableNetworkObjectId = new NetworkVariable<ulong>(
        NoInteractableNetworkObjectId,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private Camera cam;
    [SerializeField]private float distance = 3f;
    [SerializeField] private LayerMask mask;
    [SerializeField] private PlayerHUD playerHUD;
    private Interactable currentInteractable;
    private ulong lastSentInteractableNetworkObjectId = NoInteractableNetworkObjectId;

    public ulong CurrentInteractableNetworkObjectId => currentInteractableNetworkObjectId.Value;
    public bool HasNetworkInteractableTarget => CurrentInteractableNetworkObjectId != NoInteractableNetworkObjectId;

    void Awake()
    {
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

    public override void OnNetworkSpawn()
    {
        SetLocalPlayer(IsOwner);

        if (IsServer)
        {
            currentInteractableNetworkObjectId.Value = NoInteractableNetworkObjectId;
        }
    }

    public override void OnNetworkDespawn()
    {
        SetLocalPlayer(false);
    }

    public void SetLocalPlayer(bool isLocalPlayer)
    {
        if (isLocalPlayer)
        {
            return;
        }

        ClearCurrentInteractable(false);
    }

    public void CheckForInteractable()
    {
        if (!CanUseInteraction())
        {
            ClearCurrentInteractable(false);
            return;
        }

        currentInteractable = null;
        UpdatePrompt(string.Empty);
        
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance, Color.red);
        RaycastHit hitInfo;
        if(Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            if(hitInfo.collider.TryGetComponent<Interactable>(out Interactable interactable))
            {
                currentInteractable = interactable;
                UpdatePrompt("Press E to " + interactable.promptMessage);
            }
        }

        UpdateNetworkInteractableTarget();
    }

    public void Interact()
    {
        if (!CanUseInteraction() || currentInteractable == null)
        {
            return;
        }

        if (!IsSpawned)
        {
            currentInteractable.BaseInteract();
            return;
        }

        if (!TryGetNetworkObjectId(currentInteractable, out ulong targetNetworkObjectId))
        {
            if (IsServer)
            {
                currentInteractable.BaseInteract();
            }
            else
            {
                Debug.LogWarning("PlayerInteract cannot send this interaction to the server because the target has no spawned NetworkObject.", currentInteractable);
            }

            return;
        }

        if (IsServer)
        {
            InteractWithNetworkObject(targetNetworkObjectId);
            return;
        }

        InteractWithNetworkObjectRpc(targetNetworkObjectId);
    }

    private bool CanUseInteraction()
    {
        if (IsSpawned)
        {
            return IsOwner;
        }

        return Unity.Netcode.NetworkManager.Singleton == null || !HasNetworkObject;
    }

    private void ClearCurrentInteractable(bool updateNetworkState = true)
    {
        currentInteractable = null;
        UpdatePrompt(string.Empty);

        if (updateNetworkState)
        {
            UpdateNetworkInteractableTarget();
        }
    }

    private void UpdatePrompt(string promptMessage)
    {
        if (playerHUD == null)
        {
            return;
        }

        playerHUD.UpdateText(promptMessage);
    }

    private void UpdateNetworkInteractableTarget()
    {
        if (!IsSpawned)
        {
            return;
        }

        ulong targetNetworkObjectId = NoInteractableNetworkObjectId;
        TryGetNetworkObjectId(currentInteractable, out targetNetworkObjectId);

        if (targetNetworkObjectId == lastSentInteractableNetworkObjectId)
        {
            return;
        }

        lastSentInteractableNetworkObjectId = targetNetworkObjectId;

        if (IsServer)
        {
            SetNetworkInteractableTarget(targetNetworkObjectId);
            return;
        }

        SetCurrentInteractableRpc(targetNetworkObjectId);
    }

    private bool TryGetNetworkObjectId(Interactable interactable, out ulong networkObjectId)
    {
        networkObjectId = NoInteractableNetworkObjectId;

        if (interactable == null)
        {
            return false;
        }

        NetworkObject networkObject = interactable.GetComponentInParent<NetworkObject>();

        if (networkObject == null || !networkObject.IsSpawned)
        {
            return false;
        }

        networkObjectId = networkObject.NetworkObjectId;
        return true;
    }

    private void SetNetworkInteractableTarget(ulong targetNetworkObjectId)
    {
        if (!IsServer)
        {
            return;
        }

        if (targetNetworkObjectId != NoInteractableNetworkObjectId && !TryGetSpawnedNetworkObject(targetNetworkObjectId, out _))
        {
            targetNetworkObjectId = NoInteractableNetworkObjectId;
        }

        currentInteractableNetworkObjectId.Value = targetNetworkObjectId;
    }

    private void InteractWithNetworkObject(ulong targetNetworkObjectId)
    {
        if (!TryGetSpawnedNetworkObject(targetNetworkObjectId, out NetworkObject targetNetworkObject))
        {
            return;
        }

        Interactable interactable = targetNetworkObject.GetComponentInChildren<Interactable>();

        if (interactable == null)
        {
            return;
        }

        interactable.BaseInteract();
    }

    private bool TryGetSpawnedNetworkObject(ulong networkObjectId, out NetworkObject networkObject)
    {
        networkObject = null;

        if (networkObjectId == NoInteractableNetworkObjectId || NetworkManager == null || NetworkManager.SpawnManager == null)
        {
            return false;
        }

        return NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out networkObject);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void SetCurrentInteractableRpc(ulong targetNetworkObjectId)
    {
        SetNetworkInteractableTarget(targetNetworkObjectId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void InteractWithNetworkObjectRpc(ulong targetNetworkObjectId)
    {
        SetNetworkInteractableTarget(targetNetworkObjectId);
        InteractWithNetworkObject(targetNetworkObjectId);
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
