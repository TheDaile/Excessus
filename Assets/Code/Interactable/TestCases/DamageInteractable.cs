using UnityEngine;

public class DamageInteractable : Interactable
{
    protected override void Interact()
    {
        Debug.Log("Interacted with " + gameObject.name);
    }
}
