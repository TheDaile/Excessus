using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    public float playerSpeed = 5.0f;
    public float sprintMultiplier = 1.5f;
    public float gravity = -9.81f;
    public float jumpHeight = 3.0f;
    private bool groundedPlayer;
    private bool sprinting = false;
    private bool crouching = false;
    private bool lerpCrouch = false;    
    private float crouchTimer = 0f;
    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
    }

    void Update()
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
        groundedPlayer = controller.isGrounded;

        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = -2f;
        }


        Vector3 moveDirection = new Vector3(input.x, 0, input.y);
        moveDirection = transform.TransformDirection(moveDirection);
 
        float speed = sprinting ? playerSpeed * sprintMultiplier : playerSpeed;
        controller.Move(moveDirection * Time.deltaTime * speed);

        playerVelocity.y += gravity * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    public void SetSprinting(bool value) => sprinting = value;
    public void Sprint() => ToggleSprint();
    public void ToggleSprint() => SetSprinting(!sprinting);

    public void SetCrouching(bool value)
    {
        if (crouching == value) return;
        crouching = value;
        crouchTimer = 0f;
        lerpCrouch = true;
    }

    public void Crouch() => ToggleCrouch();
    public void ToggleCrouch() => SetCrouching(!crouching);

    public void Jump()
    {
        if (groundedPlayer)
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -3.0f * gravity);
        }
    }
}
