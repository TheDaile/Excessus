using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    private const float MovementInputThreshold = 0.01f;

    private CharacterController controller;
    private PlayerStats playerStats;
    private Vector3 playerVelocity;
    [SerializeField] private float playerSpeed = 5.0f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 3.0f;
    private bool groundedPlayer;
    private bool sprinting = false;
    private bool crouching = false;
    private bool lerpCrouch = false;    
    private float crouchTimer = 0f;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (lerpCrouch)
        {
            crouchTimer += Time.deltaTime;
            float p = crouchTimer / 1;
            p *= p;
            if (crouching)
            {
                controller.height = Mathf.Lerp(controller.height, 1f, p);
            }
            else
            {
                controller.height = Mathf.Lerp(controller.height, 2f, p);
            }
            if (p >= 1)
            {
                lerpCrouch = false;
                crouchTimer = 0f;
            }
        }
    }

    public void ProcessMove(Vector2 input)
    {
        ProcessMove(input, Time.deltaTime);
    }

    private void ProcessMove(Vector2 input, float deltaTime)
    {
        groundedPlayer = controller.isGrounded;

        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }


        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        moveDirection = transform.TransformDirection(moveDirection);
 
        float speed = CanSprint(input, deltaTime) ? playerSpeed * sprintMultiplier : playerSpeed;
        controller.Move(moveDirection * deltaTime * speed);

        playerVelocity.y += gravity * deltaTime;
        controller.Move(playerVelocity * deltaTime);
    }

    public void SetSprinting(bool value) => sprinting = value && !crouching;
    public void Sprint() => ToggleSprint();
    public void ToggleSprint() => SetSprinting(!sprinting);

    private bool CanSprint(Vector2 input, float deltaTime)
    {
        if (!sprinting || crouching || !HasMovementInput(input))
        {
            return false;
        }

        if (deltaTime <= 0f)
        {
            return true;
        }

        if (playerStats == null)
        {
            return false;
        }

        if (playerStats.UseSprintStamina(deltaTime))
        {
            return true;
        }

        sprinting = false;
        return false;
    }

    private bool HasMovementInput(Vector2 input) => input.sqrMagnitude > MovementInputThreshold * MovementInputThreshold;

    public void SetCrouching(bool value)
    {
        if (crouching == value) return;
        crouching = value;
        if (crouching)
        {
            sprinting = false;
        }

        crouchTimer = 0f;
        lerpCrouch = true;
    }

    public void Crouch() => ToggleCrouch();
    public void ToggleCrouch() => SetCrouching(!crouching);

    public void Jump()
    {
        if (groundedPlayer && CanJump())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }

    private bool CanJump()
    {
        if (playerStats == null)
        {
            return false;
        }

        return playerStats.UseJumpStamina();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal float PlayerSpeedForTests => playerSpeed;
    internal float VerticalVelocityForTests => playerVelocity.y;

    internal bool SetPlayerSpeedForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        playerSpeed = value;
        return true;
    }

    internal bool SetSprintMultiplierForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        sprintMultiplier = value;
        return true;
    }

    internal bool SetGravityForTests(float value)
    {
        if (value > 0f)
        {
            return false;
        }

        gravity = value;
        return true;
    }

    internal bool SetJumpHeightForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        jumpHeight = value;
        return true;
    }

    internal void SetGroundedForTests(bool value)
    {
        groundedPlayer = value;
    }

    internal void ProcessMoveForTests(Vector2 input, float deltaTime)
    {
        if (deltaTime < 0f)
        {
            throw new System.ArgumentOutOfRangeException(
                nameof(deltaTime),
                deltaTime,
                "PlayerMovement.ProcessMoveForTests requires a non-negative delta time."
            );
        }

        ProcessMove(input, deltaTime);
    }
#endif
}
