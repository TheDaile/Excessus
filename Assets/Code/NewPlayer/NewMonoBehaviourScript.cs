using UnityEngine;

public class RagdollSetup : MonoBehaviour
{
    [Header("Model z animacją (Target)")]
    public Transform animatedTargetRoot;

    [ContextMenu("Automatycznie Skonfiguruj Kosci Physics")]
    public void SetupPhysicsBones()
    {
        if (animatedTargetRoot == null)
        {
            Debug.LogError("Przypisz Ghost_Player do pola Animated Target Root!");
            return;
        }

        ConfigurableJoint[] joints = GetComponentsInChildren<ConfigurableJoint>();
        Transform[] allTargetBones = animatedTargetRoot.GetComponentsInChildren<Transform>();
        int count = 0;

        foreach (ConfigurableJoint joint in joints)
        {
            GameObject ragdollBone = joint.gameObject;

            if (!ragdollBone.TryGetComponent<PhysicsBone>(out var physicsBone))
            {
                physicsBone = ragdollBone.AddComponent<PhysicsBone>();
            }

            foreach (Transform targetBone in allTargetBones)
            {
                if (targetBone.name == ragdollBone.name)
                {
                    physicsBone.targetBone = targetBone;
                    count++;
                    break;
                }
            }
        }
        Debug.Log($"Skonfigurowano kości: {count}. Odpal grę i sprawdź!");
    }
}