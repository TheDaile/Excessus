using UnityEngine;

public class NewPlayerLook : MonoBehaviour
{
    [SerializeField] private Transform hips; 
    [SerializeField] private Transform cameraTarget; 

    void LateUpdate()
    {
        if (hips != null && cameraTarget != null)
        {
            Vector3 rotation = new Vector3(0, cameraTarget.eulerAngles.y, 0);
            hips.rotation = Quaternion.Euler(rotation);
        }
    }
}