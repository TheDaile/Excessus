using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(NewPlayerMovement))]
[RequireComponent(typeof(NewPlayerViewBodyController))]
public class NewPlayerController : MonoBehaviour
{
    private global::PlayerInput playerInput;
    private global::PlayerInput.PlayerActions playerActions;
    private NewPlayerMovement playerMovement;
    private NewPlayerViewBodyController viewBodyController;

    private void Awake()
    {
        playerInput = new global::PlayerInput();
        playerActions = playerInput.Player;

        playerMovement = GetComponent<NewPlayerMovement>();
        viewBodyController = GetComponent<NewPlayerViewBodyController>();

        playerActions.Jump.performed += OnJump;
        playerActions.Sprint.performed += OnSprintPerformed;
        playerActions.Sprint.canceled += OnSprintCanceled;
    }

    private void Update()
    {
        Vector2 lookInput = playerActions.Look.ReadValue<Vector2>();
        Vector2 moveInput = playerActions.Move.ReadValue<Vector2>();

        viewBodyController.ProcessLook(lookInput);
        playerMovement.ProcessMove(moveInput);
        viewBodyController.RefreshBodyTurn(Time.deltaTime);
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
