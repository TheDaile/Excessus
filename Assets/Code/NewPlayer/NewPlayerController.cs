using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(NewPlayerMovement))]
public class NewPlayerController : MonoBehaviour
{
    private global::PlayerInput playerInput;
    private global::PlayerInput.PlayerActions playerActions;
    private NewPlayerMovement playerMovement;

    private void Awake()
    {
        playerInput = new global::PlayerInput();
        playerActions = playerInput.Player;

        playerMovement = GetComponent<NewPlayerMovement>();

        playerActions.Jump.performed += OnJump;
        playerActions.Sprint.performed += OnSprintPerformed;
        playerActions.Sprint.canceled += OnSprintCanceled;
    }

    private void Update()
    {
        playerMovement.ProcessMove(playerActions.Move.ReadValue<Vector2>());
    }

    private void OnEnable()
    {
        EnableInput();
    }

    private void OnDisable()
    {
        DisableInput();
    }

    private void OnDestroy()
    {
        if (playerInput == null)
        {
            return;
        }

        playerActions.Jump.performed -= OnJump;
        playerActions.Sprint.performed -= OnSprintPerformed;
        playerActions.Sprint.canceled -= OnSprintCanceled;
        playerInput.Dispose();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        playerMovement.Jump();
    }

    private void OnSprintPerformed(InputAction.CallbackContext context)
    {
        playerMovement.SetSprinting(true);
    }

    private void OnSprintCanceled(InputAction.CallbackContext context)
    {
        playerMovement.SetSprinting(false);
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
