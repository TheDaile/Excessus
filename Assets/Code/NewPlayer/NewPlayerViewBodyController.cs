using UnityEngine;
using Unity.Cinemachine;

[DisallowMultipleComponent]
[RequireComponent(typeof(NewPlayerMovement))]
public class NewPlayerViewBodyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private NewPlayerMovement movement;
    [SerializeField] private Animator targetAnimator;
    [SerializeField] private Transform cameraReference;
    [SerializeField] private string cameraReferenceName = "PlayerCamera";

    private CinemachinePanTilt cameraPanTilt;
    [Header("Look")]
    [SerializeField] private float mouseYawSensitivity = 0.12f;
    [SerializeField] private bool invertCameraYaw = true;

    [Header("Body Turn")]
    [SerializeField] private float turnStartAngle = 75f;
    [SerializeField] private float turnStopAngle = 12f;
    [SerializeField] private float bodyTurnSpeed = 180f;
    [SerializeField] private float animatorTurnDampTime = 0.05f;

    private float fallbackYaw;
    private float bodyFacingYawOffset;
    private float bodyTargetFacingYaw;
    private float movementYaw;
    private bool isTurning;
    private int turnDirection;

    private void Awake()
    {
        ResolveReferences();

        float startYaw = movement != null ? movement.BodyYaw : transform.eulerAngles.y;
        fallbackYaw = cameraReference != null ? cameraReference.eulerAngles.y : startYaw;
        float startCameraYaw = GetCameraYaw();
        bodyFacingYawOffset = Mathf.DeltaAngle(startYaw, startCameraYaw);
        bodyTargetFacingYaw = startCameraYaw;
        movementYaw = startCameraYaw;

        if (movement != null)
        {
            movement.SetDesiredBodyYaw(ToRawHipsYaw(bodyTargetFacingYaw));
            movement.SetMovementYaw(movementYaw);
        }
    }

    public void RefreshBodyTurn(float deltaTime)
    {
        ResolveReferences();
        UpdateBodyTurn(deltaTime);
    }

    private void LateUpdate()
    {
        ResolveReferences();
        ApplyAnimatorState();
    }

    public void ProcessLook(Vector2 lookInput)
    {
        if (cameraReference != null)
        {
            return;
        }

        float yawInput = invertCameraYaw ? -lookInput.x : lookInput.x;
        fallbackYaw += yawInput * mouseYawSensitivity;
    }

    private void ResolveReferences()
    {
        if (movement == null)
        {
            movement = GetComponent<NewPlayerMovement>();
        }

        if (targetAnimator == null && movement != null)
        {
            targetAnimator = movement.TargetAnimator;
        }

        if (cameraReference == null && !string.IsNullOrWhiteSpace(cameraReferenceName))
        {
            GameObject cameraObject = GameObject.Find(cameraReferenceName);
            if (cameraObject != null)
            {
                cameraReference = cameraObject.transform;
            }
        }

        if (cameraPanTilt == null && cameraReference != null)
        {
            cameraPanTilt = cameraReference.GetComponent<CinemachinePanTilt>();
        }
    }

    private void UpdateBodyTurn(float deltaTime)
    {
        if (movement == null)
        {
            return;
        }

        float cameraYaw = GetCameraYaw();
        movementYaw = cameraYaw;

        float bodyYaw = GetBodyFacingYaw();
        float bodyDelta = Mathf.DeltaAngle(bodyYaw, cameraYaw);

        if (movement.HasMoveInput)
        {
            isTurning = false;
            turnDirection = 0;
            bodyTargetFacingYaw = cameraYaw;
            movement.SetDesiredBodyYaw(ToRawHipsYaw(bodyTargetFacingYaw));
            movement.SetMovementYaw(movementYaw);
            return;
        }

        if (!isTurning && Mathf.Abs(bodyDelta) >= turnStartAngle)
        {
            isTurning = true;
            bodyTargetFacingYaw = bodyYaw;
        }

        if (isTurning && Mathf.Abs(bodyDelta) <= turnStopAngle)
        {
            isTurning = false;
            turnDirection = 0;
            bodyTargetFacingYaw = bodyYaw;
        }

        if (isTurning)
        {
            turnDirection = bodyDelta >= 0f ? 1 : -1;
            bodyTargetFacingYaw = Mathf.MoveTowardsAngle(bodyTargetFacingYaw, cameraYaw, bodyTurnSpeed * deltaTime);
        }
        else
        {
            turnDirection = 0;
            bodyTargetFacingYaw = bodyYaw;
        }

        movement.SetDesiredBodyYaw(ToRawHipsYaw(bodyTargetFacingYaw));
        movement.SetMovementYaw(movementYaw);
    }

    private void ApplyAnimatorState()
    {
        if (movement == null)
        {
            return;
        }

        movement.ClearAnimatorInputOverride();

        if (targetAnimator == null)
        {
            return;
        }

        SetAnimatorFloatIfExists("Turn", turnDirection, animatorTurnDampTime);
        SetAnimatorBoolIfExists("IsTurning", isTurning);
    }

    private float GetCameraYaw()
    {
        if (cameraPanTilt != null)
        {
            float referenceYaw = 0f;

            if (cameraPanTilt.ReferenceFrame == CinemachinePanTilt.ReferenceFrames.ParentObject && cameraReference.parent != null)
            {
                referenceYaw = cameraReference.parent.eulerAngles.y;
            }

            float panYaw = invertCameraYaw ? -cameraPanTilt.PanAxis.Value : cameraPanTilt.PanAxis.Value;
            return NormalizeAngle360(referenceYaw + panYaw);
        }

        return cameraReference != null ? cameraReference.eulerAngles.y : fallbackYaw;
    }

    private float GetBodyFacingYaw()
    {
        return movement != null ? NormalizeAngle360(movement.BodyYaw + bodyFacingYawOffset) : transform.eulerAngles.y;
    }

    private float ToRawHipsYaw(float facingYaw)
    {
        return NormalizeAngle360(facingYaw - bodyFacingYawOffset);
    }

    private void SetAnimatorFloatIfExists(string parameterName, float value, float dampTime)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
        {
            return;
        }

        targetAnimator.SetFloat(parameterName, value, dampTime, Time.deltaTime);
    }

    private void SetAnimatorBoolIfExists(string parameterName, bool value)
    {
        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Bool))
        {
            return;
        }

        targetAnimator.SetBool(parameterName, value);
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (targetAnimator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in targetAnimator.parameters)
        {
            if (parameter.type == type && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private static float NormalizeAngle360(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }
}
