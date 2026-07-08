using UnityEngine;

[DisallowMultipleComponent]
public class NewPlayerMovement : MonoBehaviour
{
    private const float MovementInputThreshold = 0.01f;
    private const string InputXParameter = "InputX";
    private const string InputYParameter = "InputY";
    private const string JumpingParameter = "isJumping";
    private const string LandingParameter = "isLanding";

    [Header("Powiazane obiekty")]
    [SerializeField] private Transform animatedTargetRoot;
    [SerializeField] private Rigidbody physicalHips;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private Avatar playerAvatar;
    [SerializeField] private Transform movementReference;

    [Header("Ustawienia ruchu")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float turnSpeed = 10f;
    [SerializeField] private float jumpVelocity = 5f;

    [Header("Ustawienia Animacji (Wygładzanie)")]
    [SerializeField] private float animatorDampTime = 0.1f;

    private Vector2 moveInput;
    private bool sprinting;
    private bool jumpQueued;
    private bool isGrounded = true;
    private bool hasInputXParameter;
    private bool hasInputYParameter;
    private bool hasJumpingParameter;
    private bool hasLandingParameter;
    private Transform animatedHips;
    private bool hasDesiredBodyYaw;
    private float desiredBodyYaw;
    private bool hasMovementYaw;
    private float movementYaw;
    private bool hasAnimatorInputOverride;
    private Vector2 animatorInputOverride;

    public Transform AnimatedTargetRoot => animatedTargetRoot;
    public Rigidbody PhysicalHips => physicalHips;
    public Animator TargetAnimator => targetAnimator;
    public Vector2 MoveInput => moveInput;
    public float BodyYaw => physicalHips != null ? physicalHips.rotation.eulerAngles.y : transform.eulerAngles.y;
    public bool HasMoveInput => moveInput.sqrMagnitude > MovementInputThreshold * MovementInputThreshold;

    private void Awake()
    {
        ResolveAnimatorReferences();
        ResolveAnimatedHips();
        ConfigureAnimator();
    }

    private void FixedUpdate()
    {
        if (physicalHips == null)
        {
            return;
        }
        isGrounded = Mathf.Abs(physicalHips.linearVelocity.y) < 0.05f;

        ApplyPhysicalMovement(Time.fixedDeltaTime);
        SyncAnimatedTarget();
    }

    private void LateUpdate()
    {
        SyncAnimatedTarget();
    }

    public void ProcessMove(Vector2 input)
    {
        moveInput = Vector2.ClampMagnitude(input, 1f);

        if (targetAnimator != null)
        {
            float targetInputX = moveInput.x;
            float targetInputY = moveInput.y;

            if (moveInput.y > MovementInputThreshold) 
            {
                if (sprinting)
                {
                    targetInputY = 1.0f; 
                }
                else
                {
                    targetInputY = 0.5f; 
                }
            }
            else if (moveInput.y < -MovementInputThreshold)
            {
                targetInputY = sprinting ? -1f : -0.5f;
            }
            else
            {
                targetInputY = 0f;
            }

            if (Mathf.Abs(moveInput.x) > MovementInputThreshold && !sprinting)
            {
                targetInputX = Mathf.Sign(moveInput.x) * 0.5f;
            }

            if (hasAnimatorInputOverride)
            {
                targetInputX = animatorInputOverride.x;
                targetInputY = animatorInputOverride.y;
            }

            if (hasInputXParameter)
            {
                targetAnimator.SetFloat(InputXParameter, targetInputX, animatorDampTime, Time.deltaTime);
            }

            if (hasInputYParameter)
            {
                targetAnimator.SetFloat(InputYParameter, targetInputY, animatorDampTime, Time.deltaTime);
            }
        }
    }

    public void SetSprinting(bool value)
    {
        sprinting = value;
    }

    public void SetDesiredBodyYaw(float yaw)
    {
        desiredBodyYaw = NormalizeAngle360(yaw);
        hasDesiredBodyYaw = true;
    }

    public void SetMovementYaw(float yaw)
    {
        movementYaw = NormalizeAngle360(yaw);
        hasMovementYaw = true;
    }

    public void SetAnimatorInputOverride(Vector2 input)
    {
        animatorInputOverride = Vector2.ClampMagnitude(input, 1f);
        hasAnimatorInputOverride = true;
    }

    public void ClearAnimatorInputOverride()
    {
        hasAnimatorInputOverride = false;
        animatorInputOverride = Vector2.zero;
    }

    public void ToggleSprint() => SetSprinting(!sprinting);

    public void Jump()
    {
        if (isGrounded)
        {
            jumpQueued = true;
        }
    }

    private void ApplyPhysicalMovement(float deltaTime)
    {
        Vector3 inputDirection = GetWorldInputDirection();
        float speed = (sprinting && Mathf.Abs(moveInput.y) > MovementInputThreshold) ? moveSpeed * sprintMultiplier : moveSpeed;
        Vector3 targetVelocity = inputDirection * speed;
        Vector3 currentVelocity = physicalHips.linearVelocity;

        physicalHips.linearVelocity = new Vector3(targetVelocity.x, currentVelocity.y, targetVelocity.z);

        if (jumpQueued)
        {
            physicalHips.linearVelocity = new Vector3(
                physicalHips.linearVelocity.x,
                jumpVelocity,
                physicalHips.linearVelocity.z
            );

            SetAnimatorJumpState(true, false);

            jumpQueued = false;
        }

        else
        {
            SetAnimatorJumpState(!isGrounded, isGrounded);
        }

        float yawDelta = Mathf.Abs(Mathf.DeltaAngle(physicalHips.rotation.eulerAngles.y, GetBodyYaw()));

        if (inputDirection.sqrMagnitude > MovementInputThreshold * MovementInputThreshold || yawDelta > 0.5f)
        {
            Quaternion targetRotation = Quaternion.Euler(0f, GetBodyYaw(), 0f);
            Quaternion nextRotation = Quaternion.RotateTowards(
                physicalHips.rotation,
                targetRotation,
                turnSpeed * 90f * deltaTime
            );
            physicalHips.MoveRotation(nextRotation);
        }
    }

    private Vector3 GetWorldInputDirection()
    {
        Vector3 localInput = new Vector3(moveInput.x, 0f, moveInput.y);

        if (localInput.sqrMagnitude <= MovementInputThreshold * MovementInputThreshold)
        {
            return Vector3.zero;
        }

        Quaternion referenceRotation = Quaternion.Euler(0f, GetMovementYaw(), 0f);
        Vector3 forward = referenceRotation * Vector3.forward;
        Vector3 right = referenceRotation * Vector3.right;

        return (right * moveInput.x + forward * moveInput.y).normalized;
    }

    private float GetBodyYaw()
    {
        if (hasDesiredBodyYaw)
        {
            return desiredBodyYaw;
        }

        return movementReference != null ? movementReference.eulerAngles.y : BodyYaw;
    }

    private float GetMovementYaw()
    {
        if (hasMovementYaw)
        {
            return movementYaw;
        }

        return movementReference != null ? movementReference.eulerAngles.y : BodyYaw;
    }

    private static float NormalizeAngle360(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }

    private void SyncAnimatedTarget()
    {
        if (animatedTargetRoot == null || physicalHips == null)
        {
            return;
        }

        if (animatedHips == null)
        {
            ResolveAnimatedHips();
        }

        if (animatedHips == null)
        {
            animatedTargetRoot.SetPositionAndRotation(physicalHips.position, physicalHips.rotation);
            return;
        }

        Quaternion hipsRotationRelativeToRoot = Quaternion.Inverse(animatedTargetRoot.rotation) * animatedHips.rotation;
        Vector3 hipsPositionRelativeToRoot = Quaternion.Inverse(animatedTargetRoot.rotation) *
            (animatedHips.position - animatedTargetRoot.position);

        Quaternion targetRootRotation = physicalHips.rotation * Quaternion.Inverse(hipsRotationRelativeToRoot);
        Vector3 targetRootPosition = physicalHips.position - targetRootRotation * hipsPositionRelativeToRoot;

        animatedTargetRoot.SetPositionAndRotation(targetRootPosition, targetRootRotation);
    }

    private void ResolveAnimatorReferences()
    {
        if (targetAnimator == null && animatedTargetRoot != null)
        {
            targetAnimator = animatedTargetRoot.GetComponent<Animator>();
        }

        if (animatedTargetRoot == null && targetAnimator != null)
        {
            animatedTargetRoot = targetAnimator.transform;
        }
    }

    private void ResolveAnimatedHips()
    {
        if (animatedTargetRoot == null)
        {
            return;
        }

        animatedHips = FindChildByName(animatedTargetRoot, "Hips");
    }

    private Transform FindChildByName(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform result = FindChildByName(child, childName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    private void ConfigureAnimator()
    {
        if (targetAnimator == null)
        {
            return;
        }

        targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        targetAnimator.updateMode = AnimatorUpdateMode.Fixed;
        targetAnimator.applyRootMotion = false;

        ApplyConfiguredAvatar();
        CacheAnimatorParameters();
    }

    private void ApplyConfiguredAvatar()
    {
        if (playerAvatar == null || targetAnimator.avatar == playerAvatar)
        {
            return;
        }

        targetAnimator.avatar = playerAvatar;
        targetAnimator.Rebind();
        targetAnimator.Update(0f);
    }

    private void CacheAnimatorParameters()
    {
        hasInputXParameter = HasAnimatorParameter(InputXParameter, AnimatorControllerParameterType.Float);
        hasInputYParameter = HasAnimatorParameter(InputYParameter, AnimatorControllerParameterType.Float);
        hasJumpingParameter = HasAnimatorParameter(JumpingParameter, AnimatorControllerParameterType.Bool);
        hasLandingParameter = HasAnimatorParameter(LandingParameter, AnimatorControllerParameterType.Bool);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
            {
                return true;
            }
        }

        return false;
    }

    private void SetAnimatorJumpState(bool jumping, bool landing)
    {
        if (targetAnimator == null)
        {
            return;
        }

        if (hasJumpingParameter)
        {
            targetAnimator.SetBool(JumpingParameter, jumping);
        }

        if (hasLandingParameter)
        {
            targetAnimator.SetBool(LandingParameter, landing);
        }
    }

}
