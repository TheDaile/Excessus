using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(PlayerLook))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteract))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : NetworkBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.PlayerActions playerActions;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private PlayerInteract playerInteract;
    private Gun gun;

    public ulong PlayerOwnerClientId => IsSpawned ? OwnerClientId : 0UL;
    public bool IsControlledByLocalClient => CanUseLocalInput();

    private void Awake()
    {
        gun = GetComponentInChildren<Gun>();

        playerInput = new PlayerInput();
        playerActions = playerInput.Player;

        playerMovement = GetComponent<PlayerMovement>();
        playerLook = GetComponent<PlayerLook>();
        playerInteract = GetComponent<PlayerInteract>();

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

    public override void OnNetworkSpawn()
    {
        SetLocalControl(IsOwner);
    }

    public override void OnNetworkDespawn()
    {
        SetLocalControl(false);
    }

    private void Update()
    {
        if (!CanUseLocalInput())
        {
            return;
        }

        playerMovement.ProcessMove(playerActions.Move.ReadValue<Vector2>());
        playerLook.ProcessLook(playerActions.Look.ReadValue<Vector2>());
        playerInteract.CheckForInteractable();
        
        if (playerActions.Attack.ReadValue<float>() > 0f)
        {
            gun?.FireRate();
        }
    }

    private void OnEnable()
    {
        if (CanUseLocalInput())
        {
            EnableInput();
        }
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void OnDestroy()
    {
        playerInput?.Dispose();
    }

    private bool CanUseLocalInput()
    {
        if (IsSpawned)
        {
            return IsOwner;
        }

        return Unity.Netcode.NetworkManager.Singleton == null || !HasNetworkObject;
    }

    private void SetLocalControl(bool isLocalPlayer)
    {
        playerLook.SetLocalPlayer(isLocalPlayer);
        playerInteract.SetLocalPlayer(isLocalPlayer);

        if (isLocalPlayer && isActiveAndEnabled)
        {
            EnableInput();
        }
        else
        {
            DisableInput();
        }
    }

    private void EnableInput()
    {
        if (playerInput == null)
        {
            return;
        }

        playerActions.Enable();
    }

    private void DisableInput()
    {
        if (playerInput == null)
        {
            return;
        }

        playerActions.Disable();
    }
}
