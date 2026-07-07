using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerLook))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInteract))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerController : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.PlayerActions playerActions;
    private PlayerMovement playerMovement;
    private PlayerLook playerLook;
    private PlayerInteract playerInteract;
    private Gun gun;
    void Awake()
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

    private void Update()
    {
        playerMovement.ProcessMove(playerActions.Move.ReadValue<Vector2>());
        playerLook.ProcessLook(playerActions.Look.ReadValue<Vector2>());
        playerInteract.CheckForInteractable();
        
        if (playerActions.Attack.ReadValue<float>() > 0f)
        {
            gun.FireRate();
        }
    }


    private void OnEnable() => playerActions.Enable();
    private void OnDisable() =>  playerActions.Disable();

    
}
