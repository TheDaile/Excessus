using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NewPlayerMovement : MonoBehaviour
{
    private const float MovementInputThreshold = 0.01f;
    private const string InputXParameter = "InputX";
    private const string InputYParameter = "InputY";
    private const string JumpingParameter = "isJumping";
    private const string LandingParameter = "isLanding";

    private struct BoneMapping
    {
        public string humanName;
        public string boneName;

        public BoneMapping(string humanName, string boneName)
        {
            this.humanName = humanName;
            this.boneName = boneName;
        }
    }

    private static readonly BoneMapping[] HumanBoneMappings =
    {
        new BoneMapping("Hips", "Hips"),
        new BoneMapping("LeftUpperLeg", "LeftUpLeg"),
        new BoneMapping("RightUpperLeg", "RightUpLeg"),
        new BoneMapping("LeftLowerLeg", "LeftLeg"),
        new BoneMapping("RightLowerLeg", "RightLeg"),
        new BoneMapping("LeftFoot", "LeftFoot"),
        new BoneMapping("RightFoot", "RightFoot"),
        new BoneMapping("Spine", "Spine"),
        new BoneMapping("Chest", "Spine1"),
        new BoneMapping("UpperChest", "Spine2"),
        new BoneMapping("Neck", "Neck"),
        new BoneMapping("Head", "Head"),
        new BoneMapping("LeftShoulder", "LeftShoulder"),
        new BoneMapping("RightShoulder", "RightShoulder"),
        new BoneMapping("LeftUpperArm", "LeftArm"),
        new BoneMapping("RightUpperArm", "RightArm"),
        new BoneMapping("LeftLowerArm", "LeftForeArm"),
        new BoneMapping("RightLowerArm", "RightForeArm"),
        new BoneMapping("LeftHand", "LeftHand"),
        new BoneMapping("RightHand", "RightHand"),
        new BoneMapping("LeftToes", "LeftToeBase"),
        new BoneMapping("RightToes", "RightToeBase"),
        new BoneMapping("Left Index Proximal", "LeftHandIndex1"),
        new BoneMapping("Left Index Intermediate", "LeftHandIndex2"),
        new BoneMapping("Left Index Distal", "LeftHandIndex3"),
        new BoneMapping("Right Index Proximal", "RightHandIndex1"),
        new BoneMapping("Right Index Intermediate", "RightHandIndex2"),
        new BoneMapping("Right Index Distal", "RightHandIndex3")
    };

    [Header("Powiazane obiekty")]
    [SerializeField] private Transform animatedTargetRoot;
    [SerializeField] private Rigidbody physicalHips;
    [SerializeField] private Animator targetAnimator;
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

    private void Awake()
    {
        ResolveAnimatorReferences();
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
                targetInputY = -0.5f;
            }
            else
            {
                targetInputY = 0f;
            }

            if (Mathf.Abs(moveInput.x) > MovementInputThreshold && !sprinting)
            {
                targetInputX = Mathf.Sign(moveInput.x) * 0.5f;
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
        float speed = (sprinting && moveInput.y > MovementInputThreshold) ? moveSpeed * sprintMultiplier : moveSpeed;
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

        if (inputDirection.sqrMagnitude > MovementInputThreshold * MovementInputThreshold)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection, Vector3.up);
            Quaternion nextRotation = Quaternion.Slerp(
                physicalHips.rotation,
                targetRotation,
                deltaTime * turnSpeed
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

        Transform reference = movementReference != null ? movementReference : transform;
        Vector3 forward = Vector3.ProjectOnPlane(reference.forward, Vector3.up);
        Vector3 right = Vector3.ProjectOnPlane(reference.right, Vector3.up);

        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        if (right.sqrMagnitude < 0.0001f)
        {
            right = Vector3.right;
        }

        return (right.normalized * moveInput.x + forward.normalized * moveInput.y).normalized;
    }

    private void SyncAnimatedTarget()
    {
        if (animatedTargetRoot == null || physicalHips == null)
        {
            return;
        }

        animatedTargetRoot.position = physicalHips.position;
        animatedTargetRoot.rotation = physicalHips.rotation;
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

    private void ConfigureAnimator()
    {
        if (targetAnimator == null)
        {
            return;
        }

        targetAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        targetAnimator.updateMode = AnimatorUpdateMode.Fixed;
        targetAnimator.applyRootMotion = false;

        RebuildRuntimeAvatar();
        CacheAnimatorParameters();
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

    private void RebuildRuntimeAvatar()
    {
        if (animatedTargetRoot == null)
        {
            return;
        }

        HumanDescription description = new HumanDescription
        {
            human = BuildHumanBones(),
            skeleton = BuildSkeletonBones(),
            upperArmTwist = 0.5f,
            lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f,
            lowerLegTwist = 0.5f,
            armStretch = 0.05f,
            legStretch = 0.05f,
            feetSpacing = 0f,
            hasTranslationDoF = false
        };

        Avatar runtimeAvatar = AvatarBuilder.BuildHumanAvatar(animatedTargetRoot.gameObject, description);

        if (runtimeAvatar == null || !runtimeAvatar.isValid || !runtimeAvatar.isHuman)
        {
            Debug.LogError("Runtime humanoid avatar could not be built for animated target.", this);
            return;
        }

        runtimeAvatar.name = animatedTargetRoot.name + "_RuntimeAvatar";
        targetAnimator.avatar = runtimeAvatar;
        targetAnimator.Rebind();
        targetAnimator.Update(0f);
    }

    private HumanBone[] BuildHumanBones()
    {
        Dictionary<string, Transform> targetBones = BuildTargetBoneLookup();
        List<HumanBone> humanBones = new List<HumanBone>(HumanBoneMappings.Length);

        foreach (BoneMapping mapping in HumanBoneMappings)
        {
            if (!targetBones.ContainsKey(mapping.boneName))
            {
                Debug.LogWarning("Missing humanoid bone on animated target: " + mapping.boneName, this);
                continue;
            }

            humanBones.Add(new HumanBone
            {
                humanName = mapping.humanName,
                boneName = mapping.boneName,
                limit = new HumanLimit { useDefaultValues = true }
            });
        }

        return humanBones.ToArray();
    }

    private Dictionary<string, Transform> BuildTargetBoneLookup()
    {
        Transform[] transforms = animatedTargetRoot.GetComponentsInChildren<Transform>(true);
        Dictionary<string, Transform> targetBones = new Dictionary<string, Transform>(transforms.Length);

        foreach (Transform targetBone in transforms)
        {
            if (!targetBones.ContainsKey(targetBone.name))
            {
                targetBones.Add(targetBone.name, targetBone);
            }
        }

        return targetBones;
    }

    private SkeletonBone[] BuildSkeletonBones()
    {
        Transform[] transforms = animatedTargetRoot.GetComponentsInChildren<Transform>(true);
        List<SkeletonBone> skeletonBones = new List<SkeletonBone>(transforms.Length);

        foreach (Transform targetBone in transforms)
        {
            skeletonBones.Add(new SkeletonBone
            {
                name = targetBone.name,
                position = targetBone.localPosition,
                rotation = targetBone.localRotation,
                scale = targetBone.localScale
            });
        }

        return skeletonBones.ToArray();
    }
}
