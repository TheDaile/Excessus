using UnityEngine;

public class PhysicsBone : MonoBehaviour
{
    public Transform targetBone; 

    private ConfigurableJoint joint;
    private Quaternion startingRotation;

    void Start()
    {
        joint = GetComponent<ConfigurableJoint>();
        startingRotation = transform.localRotation;
    }

    void FixedUpdate()
    {
        if (targetBone == null || joint == null) return;
        joint.targetRotation = CopyRotation();
    }

    private Quaternion CopyRotation()
    {
        return Quaternion.Inverse(targetBone.localRotation) * startingRotation;
    }
}